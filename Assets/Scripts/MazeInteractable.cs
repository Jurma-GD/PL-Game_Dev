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
        "A faint glow lingers here. Someone was here before.",
        "Carved into the stone: 'Turn back.'",
        "The air feels heavier in this chamber.",
        "Old markings cover the walls. A warning?",
        "You hear a distant rumble. The maze shifts...",
        "A broken compass lies on the ground.",
        "The stones here are warm to the touch.",
        "Scratches on the floor. Something was dragged.",
        "A faded map fragment. Mostly illegible.",
        "The ceiling drips with condensation.",
        "An echo of footsteps. Not yours.",
        "Moss grows in strange spiral patterns here.",
        "A rusted key. It fits nothing you've found.",
        "The walls here are smoother. Intentional.",
        "Dust swirls in an unseen draft.",
        "A pile of stones, carefully stacked.",
        "The floor tiles form an arrow pointing north.",
        "Cobwebs thick as curtains block the corner.",
        "A single torch bracket, long since empty.",
        "The stone here has a bluish tint.",
        "Claw marks. Deep ones.",
        "A small alcove with a dried flower.",
        "The passage narrows then opens wide.",
        "Symbols etched in a language you don't know.",
        "A cracked mirror reflects nothing.",
        "The ground is scorched in a perfect circle.",
        "Water trickles from a crack in the wall.",
        "A child's drawing scratched into stone.",
        "The air smells of copper and dust.",
        "Three notches in the doorframe. A count?",
        "A hollow sound when you tap the wall.",
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
