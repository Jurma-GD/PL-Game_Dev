using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// A lantern the player can pick up by pressing E when nearby.
/// Increases the fog-of-war view radius so the player can see their path.
/// Spawned near the player at game start.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LanternPickup : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer lanternSprite;
    public TextMeshPro promptText;

    [Header("Settings")]
    public float expandedViewRadius = 4f;

    private bool playerInRange;
    private bool pickedUp;

    private void Start()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && !pickedUp && Input.GetKeyDown(KeyCode.E))
            PickUp();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (pickedUp) return;
        if (!collision.CompareTag("Player")) return;

        playerInRange = true;
        if (promptText != null)
        {
            promptText.text = "[E] Pick up lantern";
            promptText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        playerInRange = false;
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void PickUp()
    {
        pickedUp = true;

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // Expand fog view radius
        FogOfWar fog = FindFirstObjectByType<FogOfWar>();
        if (fog != null)
            fog.viewRadius = expandedViewRadius;

        // Show pickup message
        MegaMazeGameManager manager = FindFirstObjectByType<MegaMazeGameManager>();
        if (manager != null)
            manager.ShowLanternPickupMessage();

        // Destroy the lantern object
        Destroy(gameObject);
    }
}
