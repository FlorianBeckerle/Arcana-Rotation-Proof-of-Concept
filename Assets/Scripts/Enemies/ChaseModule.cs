using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaseModule : MonoBehaviour, IEnemyModule
{
    private EnemyContext c;

    [Header("Chase")]
    [SerializeField] private float repathInterval = 0.15f; // how often to refresh destination
    [SerializeField] private float stopDistance = 0.5f;    // stop a bit before the player

    [Header("Turning")]
    [SerializeField] private float turnSpeedDeg = 110f;    // faster turn for chase

    private float nextRepath;

    public void Init(EnemyContext ctx)
    {
        c = ctx;
        if (c.agent != null)
        {
            c.agent.speed = c.stats.moveSpeed;  // full speed
            c.agent.acceleration = Mathf.Max(10f, c.agent.speed * 12f);
            c.agent.autoBraking = false;
            c.agent.updateRotation = false;     // manual turning
            c.agent.stoppingDistance = stopDistance;
        }
    }

    public float Score()
    {
        float dist = c.distanceToPlayer();

        // Out of detection range → never chase
        if (dist > c.stats.sightRange) 
            return 0f;

        // No line of sight → never chase
        if (!c.hasLOS()) 
            return 0f;

        // Optional: make sure the path is buildable
        if (!c.agent.CalculatePath(c.player.position, new NavMeshPath()) ||
            !c.agent.pathPending && c.agent.pathStatus != NavMeshPathStatus.PathComplete)
            return 0f;

        // Closer targets give slightly higher score, always above patrol's 0.6
        return Mathf.Max(1f, 200f - dist);
    }

    public void Tick()
    {
        if (c.agent == null || !c.agent.isOnNavMesh) return;

        // Refresh destination periodically (and immediately if path is bad)
        if (Time.time >= nextRepath || !c.agent.hasPath || c.agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            nextRepath = Time.time + repathInterval;
            Vector3 dest = c.player.position;
            if (c.agent.isOnNavMesh)
                c.agent.SetDestination(dest);
        }

        // Rotate toward movement or directly toward player if nearly stationary
        Vector3 forwardHint = c.agent.desiredVelocity.sqrMagnitude > 0.001f
            ? c.agent.desiredVelocity
            : (c.player.position - c.self.position);

        forwardHint.y = 0f;
        if (forwardHint.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forwardHint, Vector3.up);
            c.self.rotation = Quaternion.RotateTowards(c.self.rotation, targetRot, turnSpeedDeg * Time.deltaTime);
        }

        // Optional animation
        c.animator?.Play("Run");
    }
}
