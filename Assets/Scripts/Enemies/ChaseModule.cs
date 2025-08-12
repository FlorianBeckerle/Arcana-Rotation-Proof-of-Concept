using UnityEngine;

public class ChaseModule : MonoBehaviour, IEnemyModule
{
    private EnemyContext c;

    public void Init(EnemyContext ctx) { c = ctx; c.agent.speed = c.stats.moveSpeed; }

    public float Score()
    {
        float d = c.distanceToPlayer();
        // Chase if seen and outside attack range; score grows as player gets closer
        return (d <= c.stats.sightRange && d > c.stats.attackRange) ? (100f - d) : 0f;
    }

    public void Tick()
    {
        c.agent.SetDestination(c.player.position);
        c.animator.Play("Run");
    }
}
