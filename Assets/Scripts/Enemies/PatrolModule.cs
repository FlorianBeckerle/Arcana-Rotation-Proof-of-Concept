using UnityEngine;
using UnityEngine.AI;

public class PatrolModuleNavMesh : MonoBehaviour, IEnemyModule
{
    [Header("Patrol")]
    [SerializeField] float stepDistance = 3f;     // how far to try per leg
    [SerializeField] float arrivalTolerance = 0.2f;
    [SerializeField] float repathInterval = 0.25f;
    [SerializeField] float patrolSpeedMul = 0.7f; // % of stats.moveSpeed while patrolling

    [Header("Side-View (2D)")]
    [SerializeField] bool lockZToStart = true;    // keep Z fixed for side-scroller feel

    private EnemyContext c;
    private int dir = 1;                          // +1 right, -1 left
    private float lockedZ;
    private float nextRepathTime;
    private Vector3 targetPos;
    private readonly NavMeshPath tmpPath = new();

    public void Init(EnemyContext ctx)
    {
        c = ctx;
        if (c.agent != null)
        {
            c.agent.speed = c.stats.moveSpeed * patrolSpeedMul;
            c.agent.updateRotation = false;   // optional for 2D/side-on
            c.agent.autoBraking = true;
        }
        if (lockZToStart) lockedZ = c.self.position.z;
        PickNextTarget();
    }

    public float Score()
    {
        // Only patrol if player not in LOS (or you can add distance checks)
        return c.hasLOS() ? 0f : 1f;
    }

    public void Tick()
    {
        if (c.agent == null) return;

        // keep agent on our locked Z plane if requested
        if (lockZToStart)
        {
            var p = c.self.position;
            if (Mathf.Abs(p.z - lockedZ) > 0.001f)
            {
                p.z = lockedZ;
                c.self.position = p;
            }
        }

        // arrived?
        if (!c.agent.pathPending && c.agent.remainingDistance <= arrivalTolerance)
        {
            FlipDir();
            PickNextTarget();
            return;
        }

        // path blocked / invalid → flip and try the other way
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;

            if (!c.agent.hasPath || c.agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                FlipDir();
                PickNextTarget();
                return;
            }

            // Optional: cheap ahead probe — if the next step can’t be built, flip
            if (!CanBuildPath(targetPos))
            {
                FlipDir();
                PickNextTarget();
            }
        }

        // Face movement dir (flip scale)
        var v = c.agent.velocity;
        if (Mathf.Abs(v.x) > 0.01f)
        {
            var s = c.self.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(v.x);
            c.self.localScale = s;
        }

        // Optional anim
        c.animator?.Play("Walk");
    }

    private void FlipDir() => dir = -dir;

    private void PickNextTarget()
    {
        Vector3 pos = c.self.position;
        Vector3 desired = pos + Vector3.right * dir * stepDistance;

        if (lockZToStart) desired.z = lockedZ;

        // Snap destination to the navmesh near desired
        if (NavMesh.SamplePosition(desired, out var hit, stepDistance, NavMesh.AllAreas))
        {
            targetPos = hit.position;
            if (lockZToStart) targetPos.z = lockedZ;

            // If even this short leg can’t build a complete path, flip and try once
            if (!CanBuildPath(targetPos))
            {
                FlipDir();
                desired = pos + Vector3.right * dir * stepDistance;
                if (lockZToStart) desired.z = lockedZ;

                if (NavMesh.SamplePosition(desired, out hit, stepDistance, NavMesh.AllAreas) && CanBuildPath(hit.position))
                {
                    targetPos = hit.position;
                    if (lockZToStart) targetPos.z = lockedZ;
                }
            }

            c.agent.SetDestination(targetPos);
        }
        else
        {
            // Nowhere to go this way → flip immediately
            FlipDir();
        }
    }

    private bool CanBuildPath(Vector3 dest)
    {
        return c.agent.CalculatePath(dest, tmpPath) && tmpPath.status == NavMeshPathStatus.PathComplete;
    }
}
