using UnityEngine;
using UnityEngine.AI;

public class WolfController : EnemyBase
{
    // Estados do comportamento
    private enum WolfState { Patrolling, Chasing, Attacking, Investigating }
    private WolfState currentState;

    [Header("Componentes")]
    private NavMeshAgent agent;
    
    [Header("Visual (Alinhamento ao Chão)")]
    public Transform modelTransform; // Arraste o objeto FILHO (Modelo 3D)
    public LayerMask groundLayer;    // Layer "Terrain"

    [Header("Configurações de Percepção")]
    public float chaseRange = 10f;          // Visão normal
    public float attackRange = 2f;          // Distância de ataque
    public float patrolRadius = 20f;        // Área de patrulha
    
    // --- NOVO: PERCEPÇÃO REDUZIDA ---
    [Tooltip("Distância para ser detectado enquanto o lobo está distraído investigando.")]
    public float distractedDetectionRange = 3.5f; 

    [Header("Investigação de Som")]
    public float tempoInvestigando = 4f; // Tempo parado olhando o local do som
    private Vector3 localDoBarulho;
    private float timerInvestigacao;

    [Header("Movimentação Natural")]
    public float minPatrolSpeed = 1.5f;
    public float maxPatrolSpeed = 4.0f;
    public float runSpeed = 6.5f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 6f;
    
    // Variáveis internas de controle
    private float timerPatrulha;
    private float currentWaitTime;
    private bool isWaiting = false;
    private float movementWarmupTimer = 0f; // Impede parada imediata ao iniciar movimento

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();

        if (agent == null) { Debug.LogError("NavMeshAgent não encontrado!"); return; }

        // Configurações Físicas
        agent.acceleration = 8f; 
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1.5f;
        agent.autoBraking = true; 

        currentState = WolfState.Patrolling;
        StartWaiting();
    }

    // --- MÉTODO CHAMADO PELA MAÇÃ ---
    public void OuvirBarulho(Vector3 local)
    {
        // Prioridade máxima: Se já está atacando, ignora distrações
        if (currentState == WolfState.Attacking) return;

        // Interrompe o que estava fazendo e vai investigar
        currentState = WolfState.Investigating;
        localDoBarulho = local;
        
        // Configura corrida até o local
        isWaiting = false;
        movementWarmupTimer = 0f;
        agent.isStopped = false;
        agent.speed = runSpeed; 
        agent.SetDestination(localDoBarulho);
        
        Debug.Log("Barulho ouvido! Investigando...");
    }

    void Update()
    {
        if (playerTransform == null || agent == null) return;
        
        // Atualiza o timer de aquecimento (previne bugs de animação)
        if (movementWarmupTimer > 0) movementWarmupTimer -= Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // --- MÁQUINA DE ESTADOS (LÓGICA DE DECISÃO) ---

        // 1. INSTINTO DE DEFESA (Prioridade Absoluta)
        if (distanceToPlayer <= attackRange)
        {
            currentState = WolfState.Attacking;
        }
        
        // 2. ESTADO DE INVESTIGAÇÃO (Distraído)
        else if (currentState == WolfState.Investigating)
        {
            // Enquanto investiga, o raio de detecção é MENOR.
            // Só persegue se o player quebrar a "zona de segurança" reduzida.
            if (distanceToPlayer <= distractedDetectionRange)
            {
                Debug.Log("Player muito perto! Distração cancelada.");
                currentState = WolfState.Chasing;
            }
            // Caso contrário, continua Investigando (ignora o chaseRange normal)
        }

        // 3. COMPORTAMENTO PADRÃO (Visão Normal)
        else if (distanceToPlayer <= chaseRange)
        {
            currentState = WolfState.Chasing;
        }
        
        // 4. NADA ACONTECENDO
        else
        {
            currentState = WolfState.Patrolling;
        }

        // --- EXECUÇÃO DO COMPORTAMENTO ---
        switch (currentState)
        {
            case WolfState.Patrolling:    PatrolLogic(); break;
            case WolfState.Chasing:       ChaseLogic(); break;
            case WolfState.Attacking:     AttackLogic(); break;
            case WolfState.Investigating: InvestigatingLogic(); break;
        }

        UpdateAnimation();
        AlignToGround();
    }

    // --- LÓGICA DE INVESTIGAÇÃO ---
    void InvestigatingLogic()
    {
        // Verifica se chegou ao local do barulho
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Chegou! Para e fica olhando/cheirando
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            isWaiting = true; // Ativa animação de Idle

            timerInvestigacao += Time.deltaTime;

            // Tempo de distração acabou?
            if (timerInvestigacao >= tempoInvestigando)
            {
                timerInvestigacao = 0f;
                currentState = WolfState.Patrolling; // Volta à vida normal
                StartWaiting(); // Começa parado para decidir próximo ponto
            }
        }
    }

    // --- LÓGICA DE PATRULHA ---
    void PatrolLogic()
    {
        if (movementWarmupTimer > 0) return; // Ignora parada se acabou de começar a andar

        // Chegou no destino?
        if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartWaiting();
        }

        if (isWaiting)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
            timerPatrulha += Time.deltaTime;

            if (timerPatrulha >= currentWaitTime)
            {
                StopWaitingAndMove();
            }
        }
    }

    // --- LÓGICA DE PERSEGUIÇÃO ---
    void ChaseLogic()
    {
        isWaiting = false;
        movementWarmupTimer = 0f;
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(playerTransform.position);
    }

    // --- LÓGICA DE ATAQUE ---
    void AttackLogic()
    {
        isWaiting = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; 

        // Gira para o player (mantendo eixo Y travado)
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        if (Vector3.Angle(transform.forward, dir) < 20f) 
        {
            animator.SetTrigger("Attack");
        }
    }

    // --- AUXILIARES DE PATRULHA ---
    void StartWaiting()
    {
        isWaiting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        timerPatrulha = 0f;
        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
    }

    void StopWaitingAndMove()
    {
        isWaiting = false;
        agent.isStopped = false;
        
        Vector3 newPos = RandomPointOnNavMesh(transform.position, patrolRadius);
        agent.SetDestination(newPos);
        agent.speed = Random.Range(minPatrolSpeed, maxPatrolSpeed);
        
        // Dá 0.5s de "imunidade" para o NavMesh acelerar sem travar a animação
        movementWarmupTimer = 0.5f; 
    }

    // --- ANIMAÇÃO ---
    void UpdateAnimation()
    {
        // Se está no "aquecimento", força Walk (evita deslize em pose de Idle)
        if (movementWarmupTimer > 0)
        {
            animator.SetFloat("Speed", 1.0f, 0.1f, Time.deltaTime);
            return;
        }

        if (isWaiting)
        {
            animator.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
    }

    // --- ALINHAMENTO AO CHÃO (INCLINAÇÃO) ---
    void AlignToGround()
    {
        if (modelTransform == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 3.0f, groundLayer))
        {
            // Projeta a frente do lobo na inclinação do chão
            Vector3 forwardNoMorro = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            
            // Calcula rotação
            Quaternion targetRotation = Quaternion.LookRotation(forwardNoMorro, hit.normal);
            
            // Suaviza
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    // --- UTILITÁRIOS ---
    Vector3 RandomPointOnNavMesh(Vector3 origin, float range)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 rnd = Random.insideUnitSphere * range;
            rnd += origin;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rnd, out hit, range, NavMesh.AllAreas)) return hit.position;
        }
        return origin;
    }

    // --- VISUALIZAÇÃO NO EDITOR ---
    void OnDrawGizmosSelected()
    {
        // Amarelo: Onde ele te vê normalmente (10m)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Vermelho: Onde ele te morde (2m)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Azul: Onde ele te vê quando está DISTRAÍDO (3.5m)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, distractedDetectionRange);
    }
}