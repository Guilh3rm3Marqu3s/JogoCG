using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [Header("Configuração da Fruta")]
    public GameObject fruitPrefab;

    [Header("Quantidade de Frutas")]
    public int minFruits = 5;
    public int maxFruits = 11;

    [Header("Área de Spawn")]
    public float spawnRadius = 5f;
    [Tooltip("Altura inicial para o Raycast. Deve ser alta o suficiente para 'pegar' o chão.")]
    public float raycastStartHeight = 10f; 
    
    [HideInInspector] public float fruitColliderRadius;
    // Define quais camadas são consideradas "Chão válido"
    public LayerMask groundLayer; 

    // ---------------------------------------------------------
    // NOVO: Limite de tentativas para evitar travamento do Unity
    private int maxSpawnAttemptsPerFruit = 10; 
    // ---------------------------------------------------------

    void Start()
    {
        SphereCollider collider = fruitPrefab.GetComponent<SphereCollider>();
        if (collider != null)
        {
            fruitColliderRadius = collider.radius * fruitPrefab.transform.localScale.x;
        }
        else
        {
            Debug.LogError("O Prefab da Fruta deve ter um SphereCollider anexado!");
        }

        SpawnFruitsOnGround();
    }

    public void SpawnFruitsOnGround()
    {
        int numberOfFruitsToSpawn = Random.Range(minFruits, maxFruits);
        int fruitsSpawnedCount = 0;

        // Loop principal: continua até atingir a quantidade desejada
        // (ou se atingirmos um limite de segurança para não travar o jogo)
        int safetyLoopBreak = 0; 

        while (fruitsSpawnedCount < numberOfFruitsToSpawn && safetyLoopBreak < 100)
        {
            bool spawnedSuccessfully = false;

            // Tenta posicionar ESSA fruta específica até X vezes
            for (int attempt = 0; attempt < maxSpawnAttemptsPerFruit; attempt++)
            {
                // 1. Gera posição aleatória
                Vector3 randomOffset = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

                // 2. Ponto do Raycast
                Vector3 rayStart = new Vector3(spawnPosition.x, spawnPosition.y + raycastStartHeight, spawnPosition.z);
                RaycastHit hit;

                // 3. Verifica se bate no chão (GroundLayer)
                if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastStartHeight + 50f, groundLayer))
                {
                    // Achou chão válido!
                    Vector3 finalPosition = hit.point;
                    finalPosition.y += fruitColliderRadius;

                    GameObject newFruit = Instantiate(fruitPrefab, finalPosition, Quaternion.identity);

                    FruitDropController dropController = newFruit.GetComponent<FruitDropController>();
                    if (dropController != null)
                    {
                        dropController.InitialPlacement(); 
                    }

                    spawnedSuccessfully = true;
                    fruitsSpawnedCount++;
                    break; // Sai do loop de tentativas e vai para a próxima fruta
                }
            }

            if (!spawnedSuccessfully)
            {
                //Debug.LogWarning("Não foi possível encontrar um local válido para uma fruta após várias tentativas.");
            }

            safetyLoopBreak++;
        }

        Debug.Log($"Total de frutas spawnadas: {fruitsSpawnedCount} de {numberOfFruitsToSpawn} desejadas.");
    }

    // ---------------------------------------------------------
    // NOVO: Desenha a área no Editor para você ver onde as frutas podem cair
    // ---------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Desenha um círculo na altura do objeto spawner
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}