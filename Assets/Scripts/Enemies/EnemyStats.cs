using UnityEngine;
using UnityEngine.AI; // or Rigidbody2D, etc.

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    public float eyeHeight = 1f;
    public float sightRange = 12f;
    public float attackRange = 2.2f;
    public float moveSpeed = 3.5f;
    public float attackCooldown = 1.0f;
    
    [Header("Rewards")]
    public int currencyDrops = 10; // how much this enemy drops on death
}
