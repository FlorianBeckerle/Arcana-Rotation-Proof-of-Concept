using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyCore : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private EnemyDebugGizmo debugGizmo;

    private readonly List<IEnemyModule> modules = new();
    private EnemyContext ctx;
    private IEnemyModule active;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (debugGizmo == null) debugGizmo = GetComponent<EnemyDebugGizmo>();

        ctx = new EnemyContext
        {
            self = transform,
            player = player,
            animator = GetComponent<Animator>(),
            agent = agent,
            stats = stats,
            hasLOS = () => Check(
                transform,
                player,
                stats.sightRange,
                obstructionMask,
                WorldManager.Instance.In2DMode, // side-scroller LOS when true
                stats.eyeHeight
            ),
            distanceToPlayer = () => Vector3.Distance(transform.position, player.position)
        };

        foreach (var m in GetComponents<IEnemyModule>())
        {
            modules.Add(m);
            m.Init(ctx);
        }
    }

    void Start()
    {
        // Make sure the agent is actually on a baked navmesh before modules tick
        StartCoroutine(PlaceAgentOnNavMeshOnce());
    }

    void Update()
    {
        // If agent still isn't on navmesh, skip module updates this frame
        if (agent != null && !agent.isOnNavMesh) return;

        // Utility/priority select: highest score wins this frame
        active = modules.OrderByDescending(m => m.Score()).FirstOrDefault();
        Debug.Log($"[EnemyCore] Active module: {(active != null ? active.GetType().Name : "None")}");
        active?.Tick();
        if(debugGizmo != null) debugGizmo.moveDir = agent.desiredVelocity;
    }

    private IEnumerator PlaceAgentOnNavMeshOnce()
    {
        if (agent == null) yield break;

        // Wait one frame in case a NavMeshSurface bakes/enables on Start
        yield return null;

        if (agent.isOnNavMesh) yield break;

        // Disable while we move transform to a valid polygon
        agent.enabled = false;

        const float searchRadius = 100f;
        if (NavMesh.SamplePosition(transform.position, out var hit, searchRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true; // re-enable once placed on the mesh
        }
        else
        {
            Debug.LogError($"{name}: No NavMesh within {searchRadius}m of {transform.position}. Check your NavMeshSurface bake/layers/base offset.");
        }
    }

    // LOS that adapts to side-scroller mode (ignore Z) vs full 3D
    public bool Check(Transform from, Transform to, float range, LayerMask obstructionMask, bool use2DSideView, float eyeHeight = 1f)
    {
        Vector3 origin = from.position + Vector3.up * eyeHeight;
        Vector3 target = to.position + Vector3.up * eyeHeight;

        if (use2DSideView)
        {
            // Ignore depth (Z); side-view like Ori/Mario
            origin.z = 0f;
            target.z = 0f;
        }

        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist > range) return false;

        return !Physics.Raycast(origin, dir.normalized, dist, obstructionMask, QueryTriggerInteraction.Ignore);
    }
}
