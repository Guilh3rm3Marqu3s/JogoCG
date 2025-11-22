using UnityEngine;
using TMPro; // Necessário para usar TextMeshPro na UI

public class PlayerThrow : MonoBehaviour
{
    [Header("Configurações de Arremesso")]
    public float throwForce = 15f;
    public Transform holdPoint; 
    public GameObject appleHeldPrefab;

    [Header("Inventário")]
    public int currentApples = 0;
    public TextMeshProUGUI appleCountText; // Arraste o texto da UI para cá

    private GameObject currentHeldAppleObject; // A maçã visual que está na mão agora
    private bool isEquipped = false; // Se a "arma" está levantada ou abaixada

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        HandleScroll();
        HandleThrow();
    }

    // Adiciona maçã ao inventário (Chamado pelo FruitCollect)
    public void AddFruit()
    {
        currentApples++;
        UpdateUI();

        // Se estivermos com a mão vazia e equipada, faz a maçã aparecer
        if (isEquipped && currentHeldAppleObject == null)
        {
            SpawnHeldApple();
        }
    }

    void HandleScroll()
    {
        // Detecta rolagem do mouse para equipar/desequipar
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f) // Rolar para cima: Equipa/Mostra
        {
            isEquipped = true;
            if (currentHeldAppleObject == null && currentApples > 0)
            {
                SpawnHeldApple();
            }
        }
        else if (scroll < 0f) // Rolar para baixo: Esconde
        {
            isEquipped = false;
            DestroyHeldApple();
        }
    }

    void HandleThrow()
    {
        // Só arremessa se tiver maçã na mão visualmente e estiver equipado
        if (isEquipped && currentHeldAppleObject != null && Input.GetMouseButtonDown(0))
        {
            ThrowFruit();
        }
    }

    void SpawnHeldApple()
    {
        if (currentApples <= 0) return;

        // Cria a maçã visual na mão
        currentHeldAppleObject = Instantiate(appleHeldPrefab, holdPoint.position, holdPoint.rotation);
        currentHeldAppleObject.transform.SetParent(holdPoint);

        // Garante que ela não tenha física enquanto está na mão
        Rigidbody rb = currentHeldAppleObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        Collider col = currentHeldAppleObject.GetComponent<Collider>();
        if (col != null) col.enabled = false; // Desativa colisão na mão para não empurrar o player
    }

    void DestroyHeldApple()
    {
        if (currentHeldAppleObject != null)
        {
            Destroy(currentHeldAppleObject);
            currentHeldAppleObject = null;
        }
    }

    void ThrowFruit()
    {
        if (currentHeldAppleObject == null) return;

        GameObject projectile = currentHeldAppleObject;
        currentHeldAppleObject = null; // Solta a referência da mão

        // Desvincula da mão
        projectile.transform.SetParent(null);

        // Reativa a física
        Collider col = projectile.GetComponent<Collider>();
        if (col != null) 
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Arremessa na direção que a câmera está olhando
            rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.VelocityChange);
        }

        // Ativa o script de barulho (se tiver) ao cair
        AppleNoise noiseScript = projectile.GetComponent<AppleNoise>();
        if(noiseScript != null) noiseScript.enabled = true;

        // Consome uma maçã
        currentApples--;
        UpdateUI();

        // Destrói depois de um tempo
        Destroy(projectile, 5f);

        // Se ainda tiver munição e estiver equipado, puxa outra maçã automaticamente após um delay (opcional)
        // Aqui faremos instantâneo:
        if (currentApples > 0 && isEquipped)
        {
            SpawnHeldApple();
        }
    }

    void UpdateUI()
    {
        if (appleCountText != null)
        {
            appleCountText.text = "x "+currentApples.ToString();
        }
    }
}