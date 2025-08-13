using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask targetLayers; // leave empty for "everything"

    private void OnTriggerEnter(Collider other) // For 3D
    {
        TryDealDamage(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) // For 2D
    {
        TryDealDamage(other.gameObject);
    }

    private void TryDealDamage(GameObject target)
    {
        // Check layer mask if needed
        if (targetLayers != 0 && (targetLayers & (1 << target.layer)) == 0)
            return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // If it's a Health script, check Kind != Player
            if (target.TryGetComponent(out Health health))
            {
                if (health.IsDead) return; // skip dead
                if (health.kind == Health.Kind.Player) return;
            }

            damageable.TakeDamage(damage);
        }
    }
}