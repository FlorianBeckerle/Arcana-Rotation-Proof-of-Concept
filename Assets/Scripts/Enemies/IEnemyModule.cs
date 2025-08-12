using UnityEngine;

public interface IEnemyModule
{
    // Called once with shared references/stats
    void Init(EnemyContext ctx);

    // Return a score (0..∞). Higher = more urgent to run right now.
    // Return <= 0 if you don't want control this frame.
    float Score();

    // Run behaviour for this frame.
    void Tick();
}
