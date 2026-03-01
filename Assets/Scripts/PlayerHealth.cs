using UnityEngine;
using TMPro;
public class PlayerHealth : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int currentHealth;
    public int maxHealth;

    public TextMeshProUGUI healthText;
    public Animator healthTextAnim;

    private void Start()
    {
        healthText.text = "HP: " + currentHealth + "/" + maxHealth;
       
    }
    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        healthTextAnim.Play("TextUpdate");

        healthText.text = "HP: " + currentHealth + "/" + maxHealth;
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

}
