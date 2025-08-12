using UnityEngine;
using UnityEngine.AI; // or Rigidbody2D, etc.

public struct EnemyContext
{
    public Transform self;
    public Transform player;
    public Animator animator;
    public NavMeshAgent agent;     
    public EnemyStats stats;
    public System.Func<bool> hasLOS;
    public System.Func<float> distanceToPlayer;
}
