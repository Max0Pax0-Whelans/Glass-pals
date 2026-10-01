using UnityEngine;

public class MovingBackAndForth : MonoBehaviour
{
    public enum MoveMode
    {
        PingPong,      // Constant speed, hard turnaround
        Sinusoidal     // Smooth ease-in/out at the ends (recommended)
    }

    [Header("Movement")]
    [Tooltip("Direction to move. (1,0,0) = right, (0,0,1) = forward, etc.")]
    public Vector3 moveDirection = Vector3.right;

    [Tooltip("How far the object moves from its starting position.")]
    public float moveDistance = 3f;

    [Tooltip("Speed. PingPong: units per second. Sinusoidal: cycles per second.")]
    public float speed = 1.5f;

    [Tooltip("Starting phase (0-1). Offset multiple objects for alternating motion.")]
    [Range(0f, 1f)]
    public float startPhase = 0f;

    [Header("Movement Mode")]
    public MoveMode moveMode = MoveMode.Sinusoidal;

    [Header("Carrying")]
    [Tooltip("If true, players and rigidbodies standing on this platform move with it.")]
    public bool carryRigidbodies = true;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.cyan;

    private Vector3 startPosition;
    private Vector3 normalizedDirection;
    private float time;

    private System.Collections.Generic.HashSet<Rigidbody> activeContacts
        = new System.Collections.Generic.HashSet<Rigidbody>();

    void Start()
    {
        startPosition = transform.position;
        normalizedDirection = moveDirection.normalized;
        time = startPhase;
    }

    void FixedUpdate()
    {
        Vector3 previousPosition = transform.position;

        if (moveMode == MoveMode.PingPong)
        {
            // Triangle wave: 0 → 1 → 0 → 1...
            time += Time.fixedDeltaTime * speed;
            float t = Mathf.PingPong(time, 1f);       // 0 to 1
            float offset = (t * 2f - 1f) * moveDistance; // -dist to +dist
            transform.position = startPosition + normalizedDirection * offset;
        }
        else // Sinusoidal
        {
            // Smooth ease-in/out
            time += Time.fixedDeltaTime * speed * Mathf.PI * 2f;
            float offset = Mathf.Sin(time) * moveDistance;
            transform.position = startPosition + normalizedDirection * offset;
        }

        // Carry any rigidbodies on top of us
        if (carryRigidbodies && activeContacts.Count > 0)
        {
            Vector3 delta = transform.position - previousPosition;
            if (delta.sqrMagnitude > 0f)
            {
                foreach (Rigidbody rb in activeContacts)
                {
                    if (rb == null || rb.isKinematic) continue;
                    rb.MovePosition(rb.position + delta);
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null)
            activeContacts.Add(collision.rigidbody);
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null)
            activeContacts.Remove(collision.rigidbody);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Vector3 dir = moveDirection.normalized;
        Vector3 endA = center - dir * moveDistance;
        Vector3 endB = center + dir * moveDistance;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(endA, endB);
        Gizmos.DrawWireSphere(endA, 0.15f);
        Gizmos.DrawWireSphere(endB, 0.15f);
    }
}