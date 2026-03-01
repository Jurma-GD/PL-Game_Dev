using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    public int damage = 1;
    // OnCollisionEnter2D is a unity method that wil fire when the enemy collides with another object.
    // This method will be used to detect when the enemy collides with the player and will deal damage to the player.

    //Collision 2d the type of collision that will be detected

    //Collision keeps track of the last collision that occurred, and contains information about the collision
    //such as the game object that was collided with, the point of contact, and the normal of the collision.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponent<PlayerHealth>().ChangeHealth(-damage);
    }
}
