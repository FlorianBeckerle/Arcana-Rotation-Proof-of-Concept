using UnityEngine;

public class AttackController : MonoBehaviour
{
    
    [Header("2D Spawn Offsets")]
    [SerializeField] private float offsetSide   = 0.7f;  // right/left
    [SerializeField] private float offsetUp     = 0.9f;  // up
    [SerializeField] private float offsetDown   = 0.7f;  // down
    [SerializeField] private bool  downOnlyInAir = true; // only allow down-attack when airborne
    
    [Header("Setup")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject hitboxPrefab;

    [Header("Tuning")]
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private float diagonalThreshold = 0.4f; // how much Y you need to count as up/down

    private PlayerInputActionHandler input;
    private float nextReady;

    void Awake()
    {
        input = GetComponent<PlayerInputActionHandler>();
        input.PrimaryAttackIntent += TryAttack;
    }

    private void TryAttack()
{
    if (Time.time < nextReady) return;

    if (WorldManager.Instance != null && WorldManager.Instance.In2DMode)
    {
        // ---- 4-way facing (favor Up whenever there's meaningful Y+) ----
        Vector2 look = input.lookDirection;
        if (look.sqrMagnitude < 0.0001f) look = Vector2.right; // default

        // thresholds: tweak if you want "up" to be easier/harder to trigger
        const float upThresh = 0.2f;
        const float downThresh = 0.2f;

        Vector2 facing;
        if (look.y > upThresh)
            facing = Vector2.up;
        else if (look.y < -downThresh)
            facing = Vector2.down;
        else
        {
            float sx = Mathf.Sign(look.x == 0f ? transform.right.x : look.x);
            if (sx == 0f) sx = 1f; // final fallback: right
            facing = new Vector2(sx, 0f);
        }

        // If grounded and facing down → convert to side slash
        bool grounded = input.isGrounded;
        if (grounded && facing == Vector2.down)
        {
            float sx = Mathf.Sign(look.x);
            if (sx == 0f) sx = Mathf.Sign(transform.right.x);
            if (sx == 0f) sx = 1f;
            facing = new Vector2(sx, 0f);
        }

        // play anim only if you have one
        if (animator) PlayAnimFor(facing);

        // ---- spawn hitbox with cardinal offsets (no diagonals) ----
        if (hitboxPrefab)
        {
            Vector3 origin = transform.position;
            Vector3 off = Vector3.zero;

            if      (facing == Vector2.right) off = new Vector3( offsetSide, 0f, 0f);
            else if (facing == Vector2.left ) off = new Vector3(-offsetSide, 0f, 0f);
            else if (facing == Vector2.up   ) off = new Vector3(0f,  offsetUp,   0f);
            else /* facing == Vector2.down */ off = new Vector3(0f, -offsetDown, 0f);

            Vector3 spawnPos = origin + off;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

            var go = Instantiate(hitboxPrefab, spawnPos, Quaternion.Euler(0, 0, angle));
            go.transform.SetParent(transform); // optional
        }
    }
    else
    {
        // TODO: 3D mode later
    }

    nextReady = Time.time + cooldown;
}



    // Map any stick/mouse direction to one of 8 cardinal/diagonal vectors
    private static Vector2 Quantize8Way(Vector2 v, float diagThresh)
    {
        v.Normalize();
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);

        // If vertical influence is small → horizontal
        if (ay < diagThresh)
            return new Vector2(Mathf.Sign(v.x), 0f);
        // If horizontal influence is small → vertical
        if (ax < diagThresh)
            return new Vector2(0f, Mathf.Sign(v.y));

        // Otherwise diagonal
        return new Vector2(Mathf.Sign(v.x), Mathf.Sign(v.y));
    }

    private void PlayAnimFor(Vector2 dir)
    {
        if (animator == null) return;
        
        // Match your animator state names here
        if      (dir == Vector2.up)                animator?.Play("Attack_Up");
        else if (dir == Vector2.down)              animator?.Play("Attack_Down");
        else if (dir == Vector2.left)              animator?.Play("Attack_Left");
        else if (dir == Vector2.right)             animator?.Play("Attack_Right");
        else if (dir.x < 0 && dir.y > 0)           animator?.Play("Attack_UpLeft");
        else if (dir.x > 0 && dir.y > 0)           animator?.Play("Attack_UpRight");
        else if (dir.x < 0 && dir.y < 0)           animator?.Play("Attack_DownLeft");
        else /* dir.x > 0 && dir.y < 0 */          animator?.Play("Attack_DownRight");
    }
}
