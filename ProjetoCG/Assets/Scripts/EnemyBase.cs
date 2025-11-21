using UnityEngine;

public class EnemyBase : MonoBehaviour
{
  
    // Variáveis comuns de vida, etc.
    public int health = 100;
    protected Animator animator;

    // A tag do seu jogador
    public string playerTag = "Player"; 
    protected Transform playerTransform;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();

        // Encontra o player no Start para todas as classes filhas
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    void Update()
    {
        
    }
}
