using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// An interactable structure inside a maze pocket.
/// Player must press E to interact. Shows dialogue and leaves a visited marker.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MazeInteractable : MonoBehaviour
{
    [HideInInspector] public int sectorIndex;
    [HideInInspector] public Vector2Int sectorCoord;

    [Header("Visuals")]
    public SpriteRenderer structureSprite;
    public SpriteRenderer visitedMarker;
    public TextMeshPro dialogueText;
    public TextMeshPro promptText;

    [Header("Settings")]
    public float dialogueDuration = 3.5f;

    // Global tracking of visited interactables
    private static HashSet<string> visitedSet = new HashSet<string>();

    private static readonly string[] dialoguePool = new string[]
    {
        "The walls whisper of forgotten paths...",
        "Carved into the stone: 'Turn back.'",
        "You hear a distant rumble. The maze shifts...",
        "A broken compass lies on the ground.",
        "Scratches on the floor. Something was dragged.",
        "An echo of footsteps. Not yours.",
        "A rusted key. It fits nothing you've found.",
        "Claw marks. Deep ones.",
        "The ground is scorched in a perfect circle.",
        "A child's drawing scratched into stone.",
    };

    private string uniqueId;
    private bool hasBeenVisited;
    private bool dialogueActive;
    private bool playerInRange;

    private void Start()
    {
        uniqueId = $"s{sectorIndex}_x{Mathf.RoundToInt(transform.position.x)}_y{Mathf.RoundToInt(transform.position.y)}";
        hasBeenVisited = visitedSet.Contains(uniqueId);

        if (structureSprite != null)
        {
            float hue = (sectorIndex * 0.0618f) % 1f;
            structureSprite.color = Color.HSVToRGB(hue, 0.5f, 0.9f);
        }

        UpdateVisitedMarker();

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
            dialogueText.sortingOrder = 20;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            promptText.sortingOrder = 20;
        }
    }

    private void Update()
    {
        if (playerInRange && !dialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        playerInRange = true;

        // Show "Press E" prompt
        if (promptText != null && !dialogueActive)
        {
            promptText.text = hasBeenVisited ? "[E] Examine again" : "[E] Examine";
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

    private void Interact()
    {
        if (!hasBeenVisited)
        {
            hasBeenVisited = true;
            visitedSet.Add(uniqueId);
            UpdateVisitedMarker();
        }

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        StartCoroutine(ShowDialogue());
    }

    private IEnumerator ShowDialogue()
    {
        dialogueActive = true;

        if (dialogueText != null)
        {
            int hash = Mathf.Abs(uniqueId.GetHashCode());
            int lineIndex = (sectorIndex + hash) % dialoguePool.Length;
            dialogueText.text = dialoguePool[lineIndex];
            dialogueText.gameObject.SetActive(true);

            yield return new WaitForSeconds(dialogueDuration);

            dialogueText.gameObject.SetActive(false);
        }

        dialogueActive = false;

        // Re-show prompt if still in range
        if (playerInRange && promptText != null)
        {
            promptText.text = "[E] Examine again";
            promptText.gameObject.SetActive(true);
        }
    }

    private void UpdateVisitedMarker()
    {
        if (visitedMarker == null) return;

        if (hasBeenVisited)
        {
            visitedMarker.gameObject.SetActive(true);
            float hue = (sectorIndex * 0.0618f + 0.5f) % 1f;
            visitedMarker.color = Color.HSVToRGB(hue, 0.8f, 1f);
        }
        else
        {
            visitedMarker.gameObject.SetActive(false);
        }
    }

    public static void ClearAllVisited()
    {
        visitedSet.Clear();
    }
}
