using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Axis to rotate around (usually Y for a horizontal platform).")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 45f;
    [Tooltip("If true, uses rotationAxis * rotationSpeed directly. If false, uses a target angle and ping-pongs.")]
    public bool continuousRotation = true;

    [Header("Oscillation (only used if continuousRotation is false)")]
    [Tooltip("Maximum angle to rotate back and forth.")]
    public float oscillationAngle = 45f;
    [Tooltip("How fast the platform oscillates.")]
    public float oscillationSpeed = 1f;

    [Header("Player Carrying")]
    [Tooltip("If true, players and rigidbodies standing on the platform rotate with it.")]
    public bool carryRigidbodies = true;

    private Quaternion startRotation;
    private float oscillationTimer = 0f;

    void Start()
    {
        startRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        if (continuousRotation)
        {
            // Constant rotation
            transform.Rotate(rotationAxis, rotationSpeed * Time.fixedDeltaTime, Space.Self);
        }
        else
        {
            // Ping-pong oscillation using sine wave
            oscillationTimer += Time.fixedDeltaTime * oscillationSpeed;
            float angle = Mathf.Sin(oscillationTimer) * oscillationAngle;
            transform.rotation = startRotation * Quaternion.AngleAxis(angle, rotationAxis);
        }
    }

    // Rotate rigidbodies standing on the platform so they move with it
    void OnCollisionStay(Collision collision)
    {
        if (!carryRigidbodies) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null || rb.isKinematic) return;

        // Compute the rotation delta this physics step
        Quaternion deltaRot = Quaternion.AngleAxis(
            continuousRotation ? rotationSpeed * Time.fixedDeltaTime : 0f,
            transform.TransformDirection(rotationAxis));

        // Rotate the rigidbody's position around the platform's pivot
        Vector3 pivotToRb = rb.position - transform.position;
        Vector3 rotatedOffset = deltaRot * pivotToRb;
        rb.MovePosition(transform.position + rotatedOffset);

        // Rotate the rigidbody's orientation too
        rb.MoveRotation(deltaRot * rb.rotation);
    }
}