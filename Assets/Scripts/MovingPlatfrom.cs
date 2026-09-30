using UnityEngine;

public class MovingObject : MonoBehaviour
{
    public enum MoveMode
    {
        PingPong,       // Moves between two points, back and forth
        Continuous,     // Loops through the same motion (same as ping-pong for up/down)
        Sinusoidal      // Smooth ease-in/out motion using a sine wave
    }

    [Header("Movement")]
    public MoveMode moveMode = MoveMode.Sinusoidal;

    [Tooltip("How far the object moves up/down from its starting position.")]
    public float moveDistance = 2f;

    [Tooltip("Speed of movement. For ping-pong: units per second. For sinusoidal: cycles per second.")]
    public float speed = 1f;

    [Tooltip("Offset the starting phase (0 to 1). Useful for syncing multiple objects.")]
    [Range(0f, 1f)]
    public float startPhase = 0f;

    [Header("Carrying")]
    [Tooltip("If true, players and rigidbodies standing on this platform move with it.")]
    public bool carryRigidbodies = true;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.green;

    private Vector3 startPosition;
    private float time;

    void Start()
    {
        startPosition = transform.position;
        time = startPhase;
    }

    void FixedUpdate()
    {
        Vector3 previousPosition = transform.position;

        if (moveMode == MoveMode.PingPong || moveMode == MoveMode.Continuous)
        {
            // Triangle wave: 0 → 1 → 0 → 1...
            time += Time.fixedDeltaTime * speed;
            float t = Mathf.PingPong(time, 1f); // 0 to 1
            float offset = (t * 2f - 1f) * moveDistance; // -moveDistance to +moveDistance
            transform.position = startPosition + Vector3.up * offset;
        }
        else // Sinusoidal
        {
            // Smooth ease-in/out using sine
            time += Time.fixedDeltaTime * speed * Mathf.PI * 2f; // convert to radians
            float offset = Mathf.Sin(time) * moveDistance;
            transform.position = startPosition + Vector3.up * offset;
        }

        // Move rigidbodies that are standing on top of us
        if (carryRigidbodies)
        {
            Vector3 delta = transform.position - previousPosition;
            if (delta.sqrMagnitude > 0f)
            {
                // Move any rigidbodies currently touching us
                foreach (var contact in activeContacts)
                {
                    if (contact == null) continue;
                    Rigidbody rb = contact;
                    if (rb.isKinematic) continue;
                    rb.MovePosition(rb.position + delta);
                }
            }
        }
    }

    // Track rigidbodies standing on us via collision messages
    private System.Collections.Generic.HashSet<Rigidbody> activeContacts = new System.Collections.Generic.HashSet<Rigidbody>();

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
        Vector3 top = center + Vector3.up * moveDistance;
        Vector3 bottom = center - Vector3.up * moveDistance;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawWireSphere(top, 0.1f);
        Gizmos.DrawWireSphere(bottom, 0.1f);
    }
}