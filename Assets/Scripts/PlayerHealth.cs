using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int health;
    public int maxHealth = 3;
    public Slider slider;

    private void Start()
    {
        health = maxHealth;

        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = health;
        }
    }

    public void ChangeHealth(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (slider != null)
            slider.value = health;

        if (health <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
