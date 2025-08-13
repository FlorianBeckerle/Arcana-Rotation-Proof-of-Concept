using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour,  IDamageable
{
    public enum Kind { Player, Enemy, Destructible }

    [Header("Identity")]
    [SerializeField] public Kind kind = Kind.Enemy;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] private bool invulnerable = false;

    [Header("Feedback (Optional)")]
    [SerializeField] private AudioClip damageSfx;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private float sfxVolume = 1f;
    // TODO: white-flash / hit-flash shader or script hook

    [Header("Enemy Drops (Optional)")]
    [Tooltip("If assigned, currencyOnDeath will be taken from this stats.asset (currencyDrops).")]
    [SerializeField] private EnemyStats enemyStats; // optional link to your SO
    [SerializeField] private int currencyOnDeath = 0; // fallback if no stats provided

    [Header("Destructible (Optional)")]
    [Tooltip("Prefab to spawn on destruction (e.g., shattered barrel).")]
    [SerializeField] private GameObject shatteredPrefab;

    [Header("Events")]
    public UnityEvent<float> OnDamaged;   // passes damage amount
    public UnityEvent OnDied;

    [SerializeField] private float currentHealth;
    private bool isDead;

    void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);

        // If an Enemy has stats, prefer its currency amount
        if (kind == Kind.Enemy && enemyStats != null)
            currencyOnDeath = enemyStats.currencyDrops;
    }

    // --- Public API ---
    public void TakeDamage(float amount)
    {
        if (isDead || invulnerable || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        // SFX + feedback
        PlayOneShot(damageSfx);
        // TODO: trigger white-flash on enemies here

        OnDamaged?.Invoke(amount);

        if (currentHealth <= 0f)
            Die();
        else
            OnStillAliveAfterHit();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void Kill()
    {
        if (isDead) return;
        currentHealth = 0f;
        Die();
    }

    public float Current => currentHealth;
    public float Max => maxHealth;
    public bool IsDead => isDead;

    // --- Internals ---
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // One last SFX
        PlayOneShot(deathSfx);

        switch (kind)
        {
            case Kind.Player:
                // TODO: player death (disable input, play death screen, respawn, etc.)
                break;

            case Kind.Enemy:
                HandleEnemyDeath();
                break;

            case Kind.Destructible:
                HandleDestructibleDeath();
                break;
        }

        OnDied?.Invoke();

        // “Invisible”: disable this object after any spawns
        gameObject.SetActive(false);
    }

    private void HandleEnemyDeath()
    {
        // Drop currency (adapt to your system)
        // Example A: direct add
        // CurrencyManager.Instance?.Add(currencyOnDeath);

        // Example B: spawn coins at this position (preferred in games)
        // TODO: replace with your real drop/spawn system:
        // CurrencySpawner.Instance?.Spawn(transform.position, currencyOnDeath);

        // If you keep EnemyCore around for other cleanup, you can do it here as well (optional).
    }

    private void HandleDestructibleDeath()
    {
        if (shatteredPrefab)
        {
            Instantiate(shatteredPrefab, transform.position, transform.rotation);
            // You can add a script on the prefab to auto-despawn it or play particles.
        }
        // Otherwise just disable (already done in Die()).
        // TODO: hook up debris physics / particles / sound variations if desired.
    }

    private void OnStillAliveAfterHit()
    {
        // Extra hooks on non-lethal damage:
        // - brief invulnerability frames
        // - small knockback
        // - TODO: play small hurt animation or flash
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, Mathf.Clamp01(sfxVolume));
    }

#if UNITY_EDITOR
    // Simple editor gizmo for current health (optional)
    void OnDrawGizmosSelected()
    {
        const float w = 1.5f;
        Vector3 a = transform.position + Vector3.up * 2f + Vector3.left * w * 0.5f;
        Vector3 b = a + Vector3.right * w;

        // background
        Gizmos.color = new Color(0,0,0,0.25f);
        Gizmos.DrawCube(a + (b-a)*0.5f, new Vector3(w, 0.08f, 0.0f));

        // bar
        float t = (Application.isPlaying && maxHealth > 0f) ? Mathf.Clamp01(currentHealth / maxHealth) : startingHealth / Mathf.Max(1f,maxHealth);
        Gizmos.color = Color.Lerp(Color.red, Color.green, t);
        Gizmos.DrawCube(a + Vector3.right * (w * t * 0.5f), new Vector3(w * t, 0.06f, 0.0f));
    }
#endif
}