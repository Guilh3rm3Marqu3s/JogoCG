using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class InteractableTrash : MonoBehaviour
{
    [Tooltip("Sound File: ")]
    public AudioClip PickupSound;
    public void OnInteract()
    {
        if (ScoreController.Instance != null)
        {
            ScoreController.Instance.RegisterInteraction();
        }

        if (PickupSound != null)
        {
            // Creates a temporary audio source at the object's position
            AudioSource.PlayClipAtPoint(PickupSound, transform.position);
        }
        
        gameObject.SetActive(false);
    }
}