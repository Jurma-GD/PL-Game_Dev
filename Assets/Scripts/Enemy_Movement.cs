using UnityEngine;

public class Enemy_Movement : MonoBehaviour
{
    [SerializeField]
    private float speed = 3f;

    private bool isChasing;
    private Rigidbody2D rb;
    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
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
