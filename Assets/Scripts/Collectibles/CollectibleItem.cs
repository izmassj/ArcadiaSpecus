using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private int pointValue = 1; // Points this collectible gives
    [SerializeField] private AudioClip collectSound; // Optional sound effect
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add points to the GameManager
            CollectibleManager.Instance.AddCollectible(pointValue);
            
            // Play sound if assigned
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
            
            // Optional particle effect
            // Destroy or disable the collectible
            gameObject.SetActive(false);
        }
    }
}