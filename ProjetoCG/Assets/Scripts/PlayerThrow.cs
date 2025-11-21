using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    public float throwForce = 10f;
    private GameObject heldFruit;

    void Update()
    {
       
       //Debug.Log(heldFruit != null);
        
        if (heldFruit != null && Input.GetMouseButtonDown(0))
        {
          
            ThrowFruit();
        }
    }

    public void SetHeldFruit(GameObject fruit)
    {
        heldFruit = fruit;
       
    }

    void ThrowFruit()
    {
        Collider fruitCollider = heldFruit.GetComponent<Collider>();
        fruitCollider.isTrigger = false;

        Debug.Log("ENTREI NO ThrowFruit()");

        heldFruit.transform.SetParent(null);

        Rigidbody rb = heldFruit.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // 🔥 Não importa se o PlayerThrow é irmão da câmera
        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.VelocityChange);

        Destroy(heldFruit, 5f);
        heldFruit = null;
    }
}
