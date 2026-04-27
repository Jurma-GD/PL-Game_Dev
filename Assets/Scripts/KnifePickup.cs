using UnityEngine;
using TMPro;
using System;

/// <summary>
/// A knife the player can pick up when they are stuck after a reshuffle.
/// Once picked up, combined with being stuck, allows the player to press R to sacrifice.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KnifePickup : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer knifeSprite;
    public TextMeshPro promptText;

    public Action OnPickedUp;

    private bool playerInRange;
    private bool pickedUp;

    private void Start()
    {
        // If no sprite assigned, create a simple visible placeholder
        if (knifeSprite == null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateKnifePlaceholder();
            sr.color = new Color(0.8f, 0.8f, 0.9f, 1f); // silver-ish
            sr.sortingOrder = 8;
            knifeSprite = sr;
            transform.localScale = Vector3.one * 0.5f;
        }

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private Sprite CreateKnifePlaceholder()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.clear);

        // Draw a simple diagonal knife shape
        for (int i = 4; i < 28; i++)
        {
            tex.SetPixel(i, i, Color.white);
            tex.SetPixel(i, i + 1, Color.white);
        }
        // Handle
        for (int i = 0; i < 6; i++)
        {
            tex.SetPixel(i, i, new Color(0.6f, 0.4f, 0.2f));
            tex.SetPixel(i + 1, i, new Color(0.6f, 0.4f, 0.2f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void Update()
    {
        if (playerInRange && !pickedUp && Input.GetKeyDown(KeyCode.E))
            PickUp();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (pickedUp || !collision.CompareTag("Player")) return;
        playerInRange = true;

        if (promptText != null)
        {
            promptText.text = "[E] Pick up knife";
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

        MegaMazeGameManager manager = FindFirstObjectByType<MegaMazeGameManager>();
        if (manager != null)
        {
            manager.hasKnife = true;

            if (manager.isStuck)
                manager.ShowStatusPublic("You pick up the knife. Press R to end it.", 3f);
            else
                manager.ShowStatusPublic("You picked up a knife. Maybe it'll be useful...", 3f);
        }

        OnPickedUp?.Invoke();
        Destroy(gameObject);
    }
}
