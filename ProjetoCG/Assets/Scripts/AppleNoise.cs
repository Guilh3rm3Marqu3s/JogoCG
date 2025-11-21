using UnityEngine;

public class AppleNoise : MonoBehaviour
{
    [Header("Configuração do Som")]
    public float raioDoSom = 20f; // Até onde o som viaja
    public float forcaMinimaImpacto = 2f; // Para não fazer barulho só de rolar devagar
    public LayerMask inimigoLayer; // Layer onde estão os Lobos (opcional, para otimizar)

    private bool jaFezBarulho = false; // Opcional: Se quiser que faça barulho só na primeira queda

    void OnCollisionEnter(Collision collision)
    {
        // Verifica se a batida foi forte o suficiente (evita barulho infinito rolando)
        if (collision.relativeVelocity.magnitude >= forcaMinimaImpacto)
        {
            GerarSom();
        }
    }

    void GerarSom()
    {
        // 1. Encontra todos os colisores dentro do raio do som
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, raioDoSom);

        foreach (var hitCollider in hitColliders)
        {
            // 2. Tenta pegar o script do Lobo
            WolfController lobo = hitCollider.GetComponent<WolfController>();

            if (lobo != null)
            {
                // 3. Avisa o lobo: "Vem aqui!"
                lobo.OuvirBarulho(transform.position);
            }
        }

        // Debug visual para você ver o raio do som na hora do impacto
        Debug.DrawRay(transform.position, Vector3.up * 5, Color.cyan, 2f);
    }
    
    // Desenha o raio do som no Editor para facilitar o ajuste
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, raioDoSom);
    }
}