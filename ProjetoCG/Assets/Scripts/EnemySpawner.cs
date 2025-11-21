using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    public GameObject wolfPrefab;            // Prefab do lobo
    public int numberOfWolves = 5;           // Quantidade fixa
    public float spawnRadius = 100f;         // Raio de spawn (XZ)

    [Header("Robustez")]
    public int maxAttemptsPerWolf = 30;      // tentativas antes de desistir
    public float navMeshSampleHeight = 50f;  // altura de onde amostramos o NavMesh
    public float navMeshSampleMaxDistance = 100f; // distância máxima pro SamplePosition
    public LayerMask groundLayer = ~0;       // que layers contam como "chão" no raycast
    public float snapDownExtra = 0.5f;       // valor extra pra evitar intersecção com o chão
    public bool drawGizmos = true;

    void Start()
    {
        SpawnAllWolves();
    }

    void SpawnAllWolves()
    {
        for (int i = 0; i < numberOfWolves; i++)
        {
            Vector3 spawnPos;
            bool ok = TryFindSpawnPoint(transform.position, spawnRadius, out spawnPos);

            if (ok)
            {
                // Instancia levemente acima e força snap via raycast pra evitar prefab pivôs estranhos
                GameObject inst = Instantiate(wolfPrefab, spawnPos + Vector3.up * snapDownExtra, Quaternion.identity);
                SnapInstanceToGround(inst);
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Não encontrou posição válida para o lobo #{i}. Spawnando no gerenciador como fallback.");
                GameObject inst = Instantiate(wolfPrefab, transform.position + Vector3.up * snapDownExtra, Quaternion.identity);
                SnapInstanceToGround(inst);
            }
        }
    }

    bool TryFindSpawnPoint(Vector3 origin, float range, out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttemptsPerWolf; attempt++)
        {
            // posição aleatória apenas em XZ usando insideUnitCircle
            Vector2 rnd = Random.insideUnitCircle * range;
            Vector3 highPoint = new Vector3(origin.x + rnd.x, origin.y + navMeshSampleHeight, origin.z + rnd.y);

            // procura o NavMesh mais próximo partindo de uma altura (resolve problemas de amostragem)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(highPoint, out hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
            {
                // tenta um raycast para garantir a altura real do terreno (se houver collider)
                RaycastHit rayHit;
                Vector3 rayStart = hit.position + Vector3.up * (navMeshSampleHeight * 0.5f + 1f);
                float rayDistance = navMeshSampleHeight * 2f;

                if (Physics.Raycast(rayStart, Vector3.down, out rayHit, rayDistance, groundLayer))
                {
                    result = rayHit.point;
                    if (drawGizmos) Debug.DrawRay(rayStart, Vector3.down * rayDistance, Color.green, 5f);
                    return true;
                }
                else
                {
                    // Raycast falhou: ainda assim podemos aceitar hit.position (NavMesh) — isso evita perda de pontos úteis
                    result = hit.position;
                    if (drawGizmos) Debug.DrawRay(rayStart, Vector3.down * rayDistance, Color.yellow, 5f);
                    // validar que a posição não está absurdamente acima do origin (ex.: > navMeshSampleHeight)
                    if (Mathf.Abs(result.y - origin.y) > navMeshSampleHeight + 5f)
                    {
                        // posição muito fora: tenta de novo
                        if (drawGizmos) Debug.DrawLine(origin, result, Color.red, 2f);
                        continue;
                    }
                    return true;
                }
            }

            // Se não achou NavMesh, tenta de novo
        }

        // esgotou tentativas
        result = Vector3.zero;
        return false;
    }

    void SnapInstanceToGround(GameObject instance)
    {
        if (instance == null) return;

        Vector3 start = instance.transform.position + Vector3.up * (navMeshSampleHeight * 0.5f + 1f);
        RaycastHit hit;
        float dist = navMeshSampleHeight * 2f;

        if (Physics.Raycast(start, Vector3.down, out hit, dist, groundLayer))
        {
            instance.transform.position = hit.point;
        }
        else
        {
            // sem colisão com terreno: tenta ajustar para o NavMesh mais próximo
            NavMeshHit nmHit;
            if (NavMesh.SamplePosition(instance.transform.position, out nmHit, navMeshSampleMaxDistance, NavMesh.AllAreas))
            {
                instance.transform.position = nmHit.position;
            }
            // caso contrário mantemos a posição e deixamos o warning
            else
            {
                Debug.LogWarning("[EnemySpawner] SnapInstanceToGround falhou: nem Raycast nem NavMesh encontraram chão.");
            }
        }
    }

    // Gizmos pra ajudar debug visual no editor
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
