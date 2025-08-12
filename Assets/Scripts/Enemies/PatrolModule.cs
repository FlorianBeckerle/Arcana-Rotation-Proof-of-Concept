using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PatrolModule : MonoBehaviour, IEnemyModule
{
    private EnemyContext c;

    [Header("Patrol Settings")]
    [SerializeField] private float stepDistance = 4f;        // distance per leg
    [SerializeField] private float arrivalTolerance = 0.4f;  // consider arrived when <= this
    [SerializeField] private float repathDelay = 0.5f;       // cooldown between new targets
    [SerializeField] private float navSampleRange = 2f;      // how far to search around desired
    [SerializeField] private float patrolSpeedMul = 0.7f;    // % of stats.moveSpeed

    private float repathTimer;
    private int dir = 1;                                     // +1 right / -1 left
    private Vector3 currentDest;

    public void Init(EnemyContext ctx)
    {
        c = ctx;

        if (c.agent != null)
        {
            c.agent.speed = c.stats.moveSpeed * patrolSpeedMul;
            c.agent.acceleration = Mathf.Max(8f, c.agent.speed * 10f);
            c.agent.autoBraking = false;     // prevents stop-go stutter
            c.agent.updateRotation = false;  // we flip scale for facing
            c.agent.stoppingDistance = 0f;
        }
    }

    // Small baseline so it runs unless a higher-priority module (e.g., Chase/Attack) wins
    public float Score() => c.hasLOS() ? 0.1f : 0.5f;

    public void Tick()
    {
        if (c.agent == null || !c.agent.isOnNavMesh) return;

        // Face movement (flip local scale X)
        var v = c.agent.velocity;
        if (Mathf.Abs(v.x) > 0.01f)
        {
            var s = c.self.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(v.x);
            c.self.localScale = s;
        }

        if (repathTimer > 0f) repathTimer -= Time.deltaTime;

        // If idle/no path or arrived → pick a new step
        if ((!c.agent.hasPath && !c.agent.pathPending) ||
            (c.agent.remainingDistance <= arrivalTolerance))
        {
            if (repathTimer <= 0f)
            {
                PickNextTarget();
                repathTimer = repathDelay;
            }
            return;
        }

        // If path went invalid while moving, recover
        if (c.agent.pathStatus != NavMeshPathStatus.PathComplete && repathTimer <= 0f)
        {
            PickNextTarget();
            repathTimer = repathDelay;
        }
    }

    private void PickNextTarget()
    {
        // Alternate simple left/right pacing
        dir = -dir;

        Vector3 desired = c.self.position + Vector3.right * dir * stepDistance;

        if (NavMesh.SamplePosition(desired, out var hit, navSampleRange, NavMesh.AllAreas))
        {
            currentDest = hit.position;
            if (c.agent.isOnNavMesh)
                c.agent.SetDestination(currentDest);
        }
        else
        {
            // If we can't find a point this way, try the other way immediately
            desired = c.self.position + Vector3.right * (-dir) * stepDistance;
            if (NavMesh.SamplePosition(desired, out hit, navSampleRange, NavMesh.AllAreas))
            {
                dir = -dir; // commit to the flip
                currentDest = hit.position;
                if (c.agent.isOnNavMesh)
                    c.agent.SetDestination(currentDest);
            }
        }
    }
}
