using UnityEngine;

public class AttackModule : MonoBehaviour, IEnemyModule
{
    private EnemyContext c;
    private float nextAttackTime;

    public void Init(EnemyContext ctx) { c = ctx; }

    public float Score()
    {
        float d = c.distanceToPlayer();
        bool can = Time.time >= nextAttackTime;
        return (d <= c.stats.attackRange && can) ? 999f : 0f; // trump others when ready
    }

    public void Tick()
    {
        c.agent.ResetPath();
        c.self.LookAt(new Vector3(c.player.position.x, c.self.position.y, c.player.position.z));
        c.animator.Play("Attack");
        nextAttackTime = Time.time + c.stats.attackCooldown;
        // apply damage via animation event, overlap, etc.
    }
}