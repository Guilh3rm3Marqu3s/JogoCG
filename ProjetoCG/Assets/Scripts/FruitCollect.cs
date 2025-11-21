using UnityEngine;

public class FruitCollect : MonoBehaviour
{
    [Header("Som")]
    public AudioClip collectSound;
    public float volume = 1f;

    [Header("Prefab da maçã que fica na mão")]
    public GameObject appleHeldPrefab;

    public float pickupRange = 2f;

    AudioSource audioSource;
    Transform player;

    bool collected = false;

    void Start()
    {
       
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        
        if (player == null || collected) return;

        float dist = Vector3.Distance(transform.position, player.position);
       
        if (dist <= pickupRange)
        {
           
            Collect();
        }
    }

    void Collect()
{
    collected = true;

      if (collectSound != null)
    {
        AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);
    }
    // Achar a câmera com tag MainCamera
    Camera cam = Camera.main;

    if (cam == null)
    {
        Debug.LogError("Nenhuma câmera com tag MainCamera encontrada!");
        return;
    }

    // Achar o HoldPoint como filho da câmera
    HoldPointTag holdTag = cam.GetComponentInChildren<HoldPointTag>();

    if (holdTag == null)
    {
        Debug.LogError("Nenhum HoldPointTag encontrado como filho da câmera!");
        return;
    }

    Transform holdPoint = holdTag.transform;

    // Instanciar a maçã na mão
    GameObject heldApple = Instantiate(appleHeldPrefab, holdPoint.position, holdPoint.rotation);
    heldApple.transform.SetParent(holdPoint);

    // Passar para o PlayerThrow
    player.GetComponent<PlayerThrow>().SetHeldFruit(heldApple);

    // Destruir fruit do mapa
    Destroy(gameObject);
}


}
