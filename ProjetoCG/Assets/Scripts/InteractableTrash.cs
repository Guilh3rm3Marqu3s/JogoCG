using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class InteractableTrash : MonoBehaviour
{
    public void OnInteract()
    {
        if (ScoreController.Instance != null)
        {
            // Tell GM to calculate score and reduce bar
            ScoreController.Instance.RegisterInteraction();
        }

        // Disable the object so it is "intangible, invisible and no longer interactable"
        // We use SetActive(false) instead of Destroy() so the GameManager 
        // doesn't lose count of the total if we were strictly counting list items.
        gameObject.SetActive(false);
    }
}