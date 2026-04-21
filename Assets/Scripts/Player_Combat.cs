using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public int damage = 1;
    public float weaponRange = 1;
    public LayerMask enemyLayer;
    public Transform attackPoint;
    public float knockbackForce = 5f;
   

    public void Attack()
    {
        anim.SetBool("isAttacking", true);

        // Find all enemies within attack range, skip triggers (vision colliders)
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, weaponRange, enemyLayer);
        foreach (var hit in enemies)
        {
            if (hit.isTrigger) continue;
            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.ChangeHealth(-damage);

                // Apply knockback away from attack point
                Enemy_Movement em = hit.GetComponent<Enemy_Movement>();
                if (em != null)
                {
                    Vector2 knockbackDir = (hit.transform.position - attackPoint.position).normalized;
                    em.ApplyKnockback(knockbackDir * knockbackForce);
                }
            }
        }

    }

    public void StopAttack()
    {
        anim.SetBool("isAttacking", false);
    }

    // Visualize attack range in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, weaponRange);
    }
}


