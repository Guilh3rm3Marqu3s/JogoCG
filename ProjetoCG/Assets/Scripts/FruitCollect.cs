using UnityEngine;

public class FruitCollect : MonoBehaviour
{
    [Header("Som")]
    public AudioClip collectSound;
    public float volume = 1f;
    public float pickupRange = 2f;

    Transform player;
    bool collected = false;

    void Start()
    {
        // Procura o player pela Tag
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
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

        // Toca o som no local antes de destruir
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);
        }

        // Acessa o script do player e adiciona a fruta
        if (player != null)
        {
            PlayerThrow playerInventory = player.GetComponent<PlayerThrow>();
            if (playerInventory != null)
            {
                playerInventory.AddFruit();
            }
        }

        // Destrói a maçã do chão
        Destroy(gameObject);
    }
}