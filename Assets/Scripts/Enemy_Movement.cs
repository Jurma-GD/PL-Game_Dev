using UnityEngine;

public class Enemy_Movement : MonoBehaviour
{
    [SerializeField]
    private float speed = 3f;
    private int facingDirection = -1; // 1 for right, -1 for left        

    private bool isChasing;
    private Rigidbody2D rb;
    private Transform player;
    private float knockbackTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Gradually decelerate during knockback for a smoother feel
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 8f);
            return;
        }

        if (!isChasing)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null || rb == null)
            return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * speed;

        // Flip to face the player
        if (direction.x > 0 && facingDirection == -1)
            Flip();
        else if (direction.x < 0 && facingDirection == 1)
            Flip();
    }

    public void ApplyKnockback(Vector2 force, float duration = 0.5f)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
            knockbackTimer = duration;
        }
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            player = collision.transform;
            isChasing = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isChasing = false;
            player = null;
        }
    }
}