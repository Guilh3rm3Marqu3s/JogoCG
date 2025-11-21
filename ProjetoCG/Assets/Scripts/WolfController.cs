using UnityEngine;
using UnityEngine.AI;

public class WolfController : EnemyBase
{
    // --- ESTADOS ---
    private enum WolfState { Patrolling, Chasing, Attacking }
    private WolfState currentState;

    [Header("Componentes")]
    private NavMeshAgent agent;

    [Header("Configurações de Distância")]
    public float chaseRange = 10f;   // Distância para começar a perseguir
    public float attackRange = 2f;   // Distância para atacar
    public float patrolRadius = 20f; // Raio de patrulha

    [Header("Velocidades (Naturalidade)")]
    public float minPatrolSpeed = 1.5f; // Andar lento
    public float maxPatrolSpeed = 4.0f; // Trote rápido
    public float runSpeed = 6.5f;       // Corrida (Perseguição)

    [Header("Tempos de Espera (Idle)")]
    public float minWaitTime = 2f;      // Tempo mínimo parado "pensando"
    public float maxWaitTime = 6f;      // Tempo máximo parado
    private float timerPatrulha;
    private float currentWaitTime;

    // Controle para saber se estamos no momento de pausa da patrulha
    private bool isWaiting = false;

    [Header("Ajuste de Inclinação (Visual)")]
    public LayerMask groundLayer;    // Selecione a layer "Terrain" no Inspector
    public Transform modelTransform;

    protected override void Start()
    {
        base.Start(); // Inicializa Animator e PlayerTransform do EnemyBase
        
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent não encontrado no Lobo!");
            return;
        }

        // --- CONFIGURAÇÃO FÍSICA (Importante para não parecer robô) ---
        agent.acceleration = 5f;       // Aceleração suave
        agent.angularSpeed = 120f;     // Rotação suave
        agent.stoppingDistance = 1.5f; // Para antes de colidir exatamente com o ponto
        agent.autoBraking = true; 

        // Estado inicial
        currentState = WolfState.Patrolling;
        
        // Começa parado para decidir o primeiro movimento
        StartWaiting(); 
    }

    void Update()
    {
        if (playerTransform == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // --- 1. MÁQUINA DE ESTADOS (Cérebro) ---
        if (distanceToPlayer <= attackRange)
        {
            currentState = WolfState.Attacking;
        }
        else if (distanceToPlayer <= chaseRange)
        {
            currentState = WolfState.Chasing;
        }
        else
        {
            // Se estava perseguindo e o player fugiu, volta a patrulhar
            if (currentState == WolfState.Chasing || currentState == WolfState.Attacking)
            {
                currentState = WolfState.Patrolling;
                // Ao perder o player, ele para um pouco para "olhar em volta"
                StartWaiting();
            }
        }

        // --- 2. EXECUTAR COMPORTAMENTO ---
        switch (currentState)
        {
            case WolfState.Patrolling:
                PatrolLogic();
                break;

            case WolfState.Chasing:
                ChaseLogic();
                break;

            case WolfState.Attacking:
                AttackLogic();
                break;
        }

        // --- 3. ATUALIZAR ANIMAÇÃO ---
        UpdateAnimation();

        AlignToGround();
    }

    // ---------------------------------------------------------
    // LÓGICA DE PATRULHA
    // ---------------------------------------------------------
    void PatrolLogic()
    {
        // Se NÃO está esperando e chegou perto do destino...
        if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartWaiting();
        }

        if (isWaiting)
        {
            // TRAVA O LOBO: Garante que ele não empurre ou deslize
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 

            timerPatrulha += Time.deltaTime;

            // Acabou o tempo de pensar? Hora de andar.
            if (timerPatrulha >= currentWaitTime)
            {
                StopWaitingAndMove();
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // Zera inércia imediatamente
        timerPatrulha = 0f;
        
        // Define quanto tempo vai esperar desta vez
        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
    }

    void StopWaitingAndMove()
    {
        isWaiting = false; // Sai do modo espera
        agent.isStopped = false; // Libera o freio
        
        // 1. Novo Destino
        Vector3 newPos = RandomPointOnNavMesh(transform.position, patrolRadius);
        agent.SetDestination(newPos);
        
        // 2. Nova Velocidade (Aleatória para naturalidade)
        agent.speed = Random.Range(minPatrolSpeed, maxPatrolSpeed);
    }

    // ---------------------------------------------------------
    // LÓGICA DE PERSEGUIÇÃO
    // ---------------------------------------------------------
    void ChaseLogic()
    {
        isWaiting = false;
        agent.isStopped = false;
        agent.speed = runSpeed; // Velocidade máxima
        agent.SetDestination(playerTransform.position);
    }

    // ---------------------------------------------------------
    // LÓGICA DE ATAQUE
    // ---------------------------------------------------------
    void AttackLogic()
    {
        isWaiting = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; 

        // Gira suavemente para olhar o player (sem usar LookAt brusco)
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0; // Mantém eixo Y travado
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // Checa ângulo para atacar
        float angle = Vector3.Angle(transform.forward, direction);
        if (angle < 20f) 
        {
            animator.SetTrigger("Attack");
        }
    }

    // ---------------------------------------------------------
    // SISTEMA DE ANIMAÇÃO (Onde a mágica acontece)
    // ---------------------------------------------------------
    void UpdateAnimation()
    {
        // CASO 1: Estamos parados esperando?
        if (isWaiting)
        {
            // Força Idle. DampTime 0.2f faz a transição suave de correr para parar.
            animator.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);
        }
        else
        {
            // CASO 2: Estamos nos movendo (ou querendo mover)
            float currentSpeed = agent.velocity.magnitude;

            // CORREÇÃO DO "DESLIZE":
            // Se o NavMesh tem um caminho mas ainda está acelerando (velocidade quase 0),
            // nós "mentimos" para o Animator dizendo que já estamos andando (1.0f).
            // Isso evita que o lobo saia do lugar deslizando em pose de Idle.
            if (currentSpeed < 0.5f && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
            {
                currentSpeed = 1.0f; // Valor mínimo para ativar animação de Walk
            }

            // Envia a velocidade real (ou a ajustada) para o Blend Tree
            animator.SetFloat("Speed", currentSpeed, 0.15f, Time.deltaTime);
        }
    }

    // ---------------------------------------------------------
    // UTILITÁRIOS
    // ---------------------------------------------------------
    Vector3 RandomPointOnNavMesh(Vector3 origin, float range)
    {
        // Tenta 10 vezes achar um ponto válido no NavMesh para evitar travar
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * range;
            randomDirection += origin;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return origin; // Se falhar, retorna a origem
    }

    // Debug visual no Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange); 

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }

    void AlignToGround()
    {
        if (modelTransform == null) return;

        RaycastHit hit;
        // Origem: um pouco acima para não começar debaixo da terra
        Vector3 rayOrigin = transform.position + Vector3.up;

        // Raio para baixo
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 3.0f, groundLayer))
        {
            // MÁGICA MATEMÁTICA 2.0 (Melhor para Quadrúpedes)
            
            // 1. Pega a direção que o NavMesh diz que é "Frente"
            Vector3 forwardAtual = transform.forward;

            // 2. Projeta essa frente na inclinação do morro
            // Isso cria um vetor que aponta morro acima (ou abaixo)
            Vector3 forwardNoMorro = Vector3.ProjectOnPlane(forwardAtual, hit.normal).normalized;

            // 3. Cria a rotação final usando a nova frente e a normal do chão como "Cima"
            Quaternion targetRotation = Quaternion.LookRotation(forwardNoMorro, hit.normal);

            // 4. Suaviza a transição
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}