using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class WolfController : EnemyBase
{
    private enum WolfState { Patrolling, Chasing, Attacking, Investigating }
    private WolfState currentState;

    [Header("Componentes")]
    private NavMeshAgent agent;
    private AudioSource audioSource;
    
    [Header("Visual")]
    public Transform modelTransform; 
    public LayerMask groundLayer;    

    [Header("Configurações de Percepção")]
    public float chaseRange = 10f;          
    public float attackRange = 2f;          
    public float patrolRadius = 20f;        
    
    [Header("Sons")] 
    public AudioClip howlClip; 

    [Header("Combate")]
    public int damageAmount = 10;
    public float attackRate = 1.5f; // Tempo entre ataques do lobo
    private float nextAttackTime = 0f;

    
    [Header("Percepção Reduzida")]
    public float distractedDetectionRange = 3.5f; 

    [Header("Investigação de Som")]
    public float tempoInvestigando = 4f; 
    private Vector3 localDoBarulho;
    private float timerInvestigacao;

    [Header("Movimentação")]
    public float minPatrolSpeed = 1.5f;
    public float maxPatrolSpeed = 4.0f;
    public float runSpeed = 6.5f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 6f;
    
    private float timerPatrulha;
    private float currentWaitTime;
    private bool isWaiting = false;
    private float movementWarmupTimer = 0f; 

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; 

        if (agent == null) { Debug.LogError("NavMeshAgent não encontrado!"); return; }

        agent.acceleration = 8f; 
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1.5f;
        agent.autoBraking = true; 

        currentState = WolfState.Patrolling;
        StartWaiting();
    }

    // --- CORREÇÃO AQUI ---
    public void OuvirBarulho(Vector3 local)
    {
        // Se ele já está COMBATENDO (Atacando OU Perseguindo), ele ignora a maçã.
        // O instinto de caça ao player é maior que a curiosidade.
        if (currentState == WolfState.Attacking || currentState == WolfState.Chasing) return;

        currentState = WolfState.Investigating;
        localDoBarulho = local;
        
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
        
        if (movementWarmupTimer > 0) movementWarmupTimer -= Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // --- MÁQUINA DE ESTADOS ---

        // 1. INSTINTO DE DEFESA (Prioridade Máxima)
        if (distanceToPlayer <= attackRange)
        {
            currentState = WolfState.Attacking;
        }
        
        // 2. ESTADO DE INVESTIGAÇÃO (Distraído)
        else if (currentState == WolfState.Investigating)
        {
            if (distanceToPlayer <= distractedDetectionRange)
            {
                Debug.Log("Player muito perto! Distração cancelada.");
                TryPlayHowl();
                currentState = WolfState.Chasing;
            }
        }

        // 3. COMPORTAMENTO PADRÃO (Perseguição)
        else if (distanceToPlayer <= chaseRange)
        {
            TryPlayHowl();
            currentState = WolfState.Chasing;
        }
        
        // 4. PATRULHA
        else
        {
            currentState = WolfState.Patrolling;
        }

        // --- EXECUÇÃO ---
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

    void TryPlayHowl()
    {
        // Toca o uivo apenas se ele NÃO estava em modo de combate antes
        if (currentState != WolfState.Chasing && currentState != WolfState.Attacking)
        {
            if (howlClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(howlClip);
            }
        }
    }

    void InvestigatingLogic()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            isWaiting = true; 

            timerInvestigacao += Time.deltaTime;

            if (timerInvestigacao >= tempoInvestigando)
            {
                timerInvestigacao = 0f;
                currentState = WolfState.Patrolling; 
                StartWaiting(); 
            }
        }
    }

    void PatrolLogic()
    {
        if (movementWarmupTimer > 0) return; 

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

    void ChaseLogic()
    {
        isWaiting = false;
        movementWarmupTimer = 0f;
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(playerTransform.position);
    }

    void AttackLogic()
    {
        isWaiting = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; 

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0; 
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float angle = Vector3.Angle(transform.forward, direction);
        
        // Verifica tempo e ângulo
        if (angle < 20f && Time.time >= nextAttackTime) 
        {
            // 1. Apenas inicia a animação
            animator.SetTrigger("Attack");
            
            // NOTA: Removemos o TryDoDamage() daqui!
            // O dano agora será chamado pela própria animação.

            nextAttackTime = Time.time + attackRate;
        }
    }

    public void DealDamageEvent()
    {
        // Medida de segurança: O player ainda existe?
        if (playerTransform == null) return;

        // LÓGICA DE ESQUIVA:
        // Como passou um tempinho entre o início da animação e a mordida,
        // conferimos se o player ainda está perto. Se ele correu, o ataque erra.
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Damos uma margem extra (attackRange + 0.5f) para não ser punitivo demais
        if (distance <= attackRange + 0.5f)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }

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
        
        movementWarmupTimer = 0.5f; 
    }

    void UpdateAnimation()
    {
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

    void AlignToGround()
    {
        if (modelTransform == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 3.0f, groundLayer))
        {
            Vector3 forwardNoMorro = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(forwardNoMorro, hit.normal);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, distractedDetectionRange);
    }
}