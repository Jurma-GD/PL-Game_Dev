using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    public Rigidbody2D rb;
    public Animator animator;

    /// <summary>
    /// Set to false to freeze the player (e.g. during sleep transition).
    /// </summary>
    [HideInInspector] public bool canMove = true;

    private int facingDirection = 1;

    void FixedUpdate()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
            {
                animator.SetFloat("horizontal", 0f);
                animator.SetFloat("vertical", 0f);
            }
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if ((horizontal > 0 && transform.localScale.x < 0) ||
            (horizontal < 0 && transform.localScale.x > 0))
        {
            Flip();
        }

        if (animator != null)
        {
            animator.SetFloat("horizontal", Mathf.Abs(horizontal));
            animator.SetFloat("vertical", Mathf.Abs(vertical));
        }

        rb.linearVelocity = new Vector2(horizontal, vertical).normalized * speed;
    }

    void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
