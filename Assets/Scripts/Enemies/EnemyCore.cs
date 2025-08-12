using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class EnemyCore : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask obstructionMask = ~0;

    private readonly List<IEnemyModule> modules = new();
    private EnemyContext ctx;
    private IEnemyModule active;

    void Awake()
    {
        
        ctx = new EnemyContext {
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
                WorldManager.Instance.In2DMode, // your side-view toggle
                stats.eyeHeight                 // eyeHeight already baked via 'eyes'; keep 0 if using eyes
            ),
            distanceToPlayer = () => Vector3.Distance(transform.position, player.position)
        };

        foreach (var m in GetComponents<IEnemyModule>())
        {
            modules.Add(m);
            m.Init(ctx);
        }
    }

    void Update()
    {
        // Utility/priority select: highest score wins this frame
        active = modules.OrderByDescending(m => m.Score()).FirstOrDefault();
        active?.Tick();
    }
    
    public bool Check(Transform from, Transform to, float range, LayerMask obstructionMask, bool use2DSideView, float eyeHeight = 1f)
    {
        Vector3 origin = from.position + Vector3.up * eyeHeight;
        Vector3 target = to.position + Vector3.up * eyeHeight; // keep height for side view

        if (use2DSideView)
        {
            // Ignore depth (Z-axis) so we only care about X/Y differences
            origin.z = 0f;
            target.z = 0f;
        }

        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist > range) return false;

        return !Physics.Raycast(origin, dir.normalized, dist, obstructionMask);
    }
}