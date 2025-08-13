using UnityEngine;

public class EnemyDebugGizmo : MonoBehaviour
{
    [SerializeField] private Color arrowColor = Color.cyan;
    [SerializeField] private float arrowLength = 2f;
    [SerializeField] private float arrowHeadSize = 0.3f;

    // You can set this from your patrol/chase script each frame
    [HideInInspector] public Vector3 moveDir = Vector3.zero;

    private void OnDrawGizmosSelected()
    {
        if (moveDir.sqrMagnitude < 0.0001f) return;

        Gizmos.color = arrowColor;
        Vector3 origin = transform.position;
        Vector3 target = origin + moveDir.normalized * arrowLength;

        // Main line
        Gizmos.DrawLine(origin, target);

        // Arrow head
        Vector3 right = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawLine(target, target + right * arrowHeadSize);
        Gizmos.DrawLine(target, target + left * arrowHeadSize);
    }
}