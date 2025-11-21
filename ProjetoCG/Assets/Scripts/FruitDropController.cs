using UnityEngine;

public class FruitDropController : MonoBehaviour
{
    // Mantemos o Torque para um efeito visual de "acabou de cair"
    public float initialSpinTorque = 5f; 
    
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // A fruta nasce cinemática e FICA cinemática (FIXA)
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Chamado pelo Spawner para aplicar o giro visual no momento do spawn.
    /// </summary>
    public void InitialPlacement()
    {
        // Se a fruta for usada para outros propósitos, você pode ligar temporariamente 
        // a física para o giro e desligar logo em seguida.
        
        if (rb != null)
        {
            // Aplicamos o Torque APENAS para o efeito visual de "drop"
            rb.AddTorque(Random.insideUnitSphere * initialSpinTorque, ForceMode.Impulse);
        }
    }
}