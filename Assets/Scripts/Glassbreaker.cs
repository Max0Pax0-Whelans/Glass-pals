using UnityEngine;

public class GlassHazard : MonoBehaviour
{
    [Tooltip("Layers that will shatter the glass.")]
    public LayerMask hazardLayers;

    [Header("Break Effects")]
    public GameObject shatterPrefab;
    public GameObject breakParticles;
    public AudioClip breakSound;
    public float explosionForce = 150f;

    private bool hasShattered = false;
    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        if (hasShattered) return;

        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - previousPosition;
        float distance = delta.magnitude;

        if (distance > 0.001f)
        {
            // Cast a ray along the swept path, using the glass's longest dimension
            // as the thickness so we don't miss edges.
            float thickness = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f;
            Vector3 halfDelta = delta * 0.5f;
            Vector3 origin = previousPosition - halfDelta;
            Vector3 direction = delta.normalized;
            float castDistance = distance + thickness;

            // Cast against everything on the hazard layers
            RaycastHit[] hits = Physics.SphereCastAll(
                previousPosition,
                thickness * 0.5f,
                direction,
                castDistance,
                hazardLayers,
                QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;   // ignore self
                if (hit.collider.isTrigger && !hazardLayers.HasLayer(hit.collider.gameObject.layer)) continue;
                Shatter(hit.point);
                return;
            }
        }

        previousPosition = currentPos;
    }

    void Shatter(Vector3 hitPoint)
    {
        hasShattered = true;

        if (shatterPrefab != null)
        {
            GameObject shards = Instantiate(shatterPrefab, transform.position, transform.rotation);
            shards.transform.localScale = transform.localScale;

            foreach (Rigidbody shardRb in shards.GetComponentsInChildren<Rigidbody>())
            {
                shardRb.isKinematic = false;
                shardRb.useGravity = true;
                shardRb.AddExplosionForce(explosionForce, hitPoint, 3f, 0.5f, ForceMode.Impulse);
            }
        }

        if (breakParticles != null)
        {
            GameObject fx = Instantiate(breakParticles, hitPoint, Quaternion.identity);
            Destroy(fx, 3f);
        }

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, hitPoint);

        GlassCarryManager manager = FindObjectOfType<GlassCarryManager>();
        if (manager != null)
            manager.NotifyGlassDestroyed();

        Destroy(gameObject);
    }
}

// LayerMask extension helper
public static class LayerMaskExtensions
{
    public static bool HasLayer(this LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}