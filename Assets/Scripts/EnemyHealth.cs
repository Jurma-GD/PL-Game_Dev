using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;

    private SpriteRenderer spriteRenderer;
    private Enemy_Movement enemyMovement;

    public void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyMovement = GetComponent<Enemy_Movement>();
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            // Trigger stagger on hit
            StartCoroutine(StaggerEffect());
        }
    }

    private IEnumerator StaggerEffect()
    {
        // Flash white to indicate hit
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f); // red tint
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white; // back to normal
        }
    }
}
