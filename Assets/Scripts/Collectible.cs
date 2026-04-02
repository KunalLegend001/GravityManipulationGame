using UnityEngine;

/// <summary>
/// Simple collectible script for cubes. On trigger, increments GameManager and disables object.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleCube : MonoBehaviour
{
    public AudioClip pickupSound;
    bool collected = false;

    void Start()
    {
        // Ensure collider is trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.gameObject.CompareTag("Player"))
        {
            collected = true;
            GameManager.Instance.RegisterCollectiblePickup();
            if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            gameObject.SetActive(false);
        }
    }
}
