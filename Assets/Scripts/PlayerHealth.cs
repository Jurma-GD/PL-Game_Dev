using UnityEngine;
using UnityEngine.UI;   
public class PlayerHealth : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int health;
    public int maxHealth;
    public Slider slider;

    

    private void Start()
    {
        health = maxHealth;
        slider.maxValue = maxHealth; 
        slider.value = health;

    }
    public void ChangeHealth(int amount)
    {
        health += amount;
        slider.value = health;


        if (health <= 0)
        {
            gameObject.SetActive(false);
        }
    }

}
