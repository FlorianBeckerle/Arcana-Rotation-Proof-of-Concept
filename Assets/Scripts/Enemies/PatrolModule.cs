using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PatrolModule : MonoBehaviour, IEnemyModule
{
    private EnemyContext c;

    [Header("Patrol Settings")]
    [SerializeField] private float checkDistance = 1f;     // How far ahead to check for wall/gap
    [SerializeField] private float floorCheckDepth = 2f;   // How far down to check for floor
    [SerializeField] private float repathDelay = 0.2f;     // Delay before picking new direction after turn
    [SerializeField] private float patrolSpeedMul = 0.7f;

    [Header("Turning")]
    [SerializeField] private float turnSpeedDeg = 360f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask groundMask;

    private float repathTimer;
    private Vector3 moveDir = Vector3.right; // Default direction
    private bool useSideView;

    public void Init(EnemyContext ctx)
    {
        c = ctx;

        if (c.agent != null)
        {
            c.agent.speed = c.stats.moveSpeed * patrolSpeedMul;
            c.agent.acceleration = Mathf.Max(8f, c.agent.speed * 10f);
            c.agent.autoBraking = false;
            c.agent.updateRotation = false;
            c.agent.stoppingDistance = 0f;
        }

        // Check world mode from WorldManager
        useSideView = WorldManager.Instance != null && WorldManager.Instance.In2DMode;
    }

    public float Score() => c.hasLOS() ? 0.1f : 0.5f;

    public void Tick()
    {
        if (c.agent == null || !c.agent.isOnNavMesh) return;
        
        // If Chase left a path or stopped the agent, clear it so Move() works.
        if (c.agent.hasPath || c.agent.isStopped)
        {
            c.agent.ResetPath();
            c.agent.isStopped = false;
        }

        if (repathTimer > 0f)
        {
            repathTimer -= Time.deltaTime;
            return;
        }

        // --- NavMesh edge check (pre-move) ---
        if (EdgeAheadOnNavMesh(c.agent.nextPosition, moveDir, checkDistance))
        {
            ReverseDirection();
            return; // wait one frame after flipping
        }

        // Keep moving in chosen direction
        c.agent.Move(moveDir.normalized * c.agent.speed * Time.deltaTime);

        // Rotate toward movement
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z), Vector3.up);
            c.self.rotation = Quaternion.RotateTowards(c.self.rotation, targetRot, turnSpeedDeg * Time.deltaTime);
        }

        // Obstacle ahead?
        if (Physics.Raycast(c.self.position + Vector3.up * c.stats.eyeHeight, moveDir, checkDistance, obstacleMask))
        {
            ReverseDirection();
        }
        else
        {
            // Floor check (for side-view mode)
            if (!HasFloorAhead())
            {
                ReverseDirection();
            }
        }
        
        //Proc Anim --> not needed
        //c.animator?.Play("Walk");
    }


    private void ReverseDirection()
    {
        moveDir = -moveDir;
        repathTimer = repathDelay;
    }

    private bool HasFloorAhead()
    {
        Vector3 forwardPos = c.self.position + moveDir * checkDistance;
        Vector3 downDir = Vector3.down;

        if (useSideView)
        {
            // Lock Z for side view so raycast is 2D-like
            forwardPos.z = c.self.position.z;
        }

        return Physics.Raycast(forwardPos, downDir, floorCheckDepth, groundMask);
    }
    
    private bool EdgeAheadOnNavMesh(Vector3 origin, Vector3 dir, float distance)
    {
        // Probe a short segment ahead on the navmesh.
        // If NavMesh.Raycast returns true, movement from A->B hits an edge.
        var start = origin;
        var end   = origin + dir.normalized * distance;

        // Make sure start/end are on/near the mesh so the raycast works reliably
        if (!NavMesh.SamplePosition(start, out var sHit, 0.3f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(end,   out var eHit, 0.3f, NavMesh.AllAreas)) return true; // end is off-mesh ⇒ edge

        return NavMesh.Raycast(sHit.position, eHit.position, out _, NavMesh.AllAreas);
    }

}
