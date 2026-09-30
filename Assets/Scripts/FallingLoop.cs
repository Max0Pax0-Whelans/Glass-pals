using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingLoopObject : MonoBehaviour
{
    [Header("Respawn Trigger")]
    [Tooltip("Layers that trigger a respawn when the object collides with them.")]
    public LayerMask respawnOnLayers;

    [Tooltip("If true, respawns on ANY collision instead of just the specified layers.")]
    public bool respawnOnAnyCollision = false;

    [Header("Respawn Settings")]
    [Tooltip("Delay before respawning after a collision.")]
    public float respawnDelay = 0.5f;

    [Tooltip("Optional: if true, plays a shatter/impact effect at the collision point.")]
    public bool spawnImpactEffect = false;

    [Tooltip("Optional impact effect prefab.")]
    public GameObject impactEffectPrefab;

    [Tooltip("Optional impact sound.")]
    public AudioClip impactSound;

    [Header("Loop Behavior")]
    [Tooltip("If true, the object falls immediately on respawn. If false, waits for a trigger.")]
    public bool autoFallOnRespawn = true;

    [Header("Reset Physics")]
    [Tooltip("Zero out velocity and angular velocity on respawn.")]
    public bool resetVelocity = true;

    private Rigidbody rb;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isRespawning = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isRespawning) return;

        // Check layer filter
        bool shouldRespawn = respawnOnAnyCollision
            || ((1 << collision.gameObject.layer) & respawnOnLayers) != 0;

        if (!shouldRespawn) return;

        // Get the hit point for effects
        Vector3 hitPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        StartRespawn(hitPoint);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isRespawning) return;

        bool shouldRespawn = respawnOnAnyCollision
            || ((1 << other.gameObject.layer) & respawnOnLayers) != 0;

        if (!shouldRespawn) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        StartRespawn(hitPoint);
    }

    void StartRespawn(Vector3 hitPoint)
    {
        isRespawning = true;

        // Spawn impact effect
        if (spawnImpactEffect && impactEffectPrefab != null)
        {
            GameObject fx = Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(fx, 3f);
        }

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, hitPoint);

        // Hide and stop physics during delay
        SetPhysicsActive(false);
        SetVisible(false);

        Invoke(nameof(DoRespawn), respawnDelay);
    }

    void DoRespawn()
    {
        // Reset position and rotation
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (resetVelocity)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Make visible again
        SetVisible(true);

        // Reactivate physics
        SetPhysicsActive(true);

        isRespawning = false;
    }

    void SetPhysicsActive(bool active)
    {
        rb.isKinematic = !active;
        rb.useGravity = active;

        if (active && resetVelocity)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void SetVisible(bool visible)
    {
        // Toggle renderers
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;

        // Toggle colliders
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = visible;
    }

    // Optional: manually trigger a respawn from another script
    public void TriggerRespawn()
    {
        if (!isRespawning)
            StartRespawn(transform.position);
    }
}