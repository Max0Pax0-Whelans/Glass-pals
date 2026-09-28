using UnityEngine;

public class CoopFollowCamera : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;

    [Header("Follow Settings")]
    public float followSmoothTime = 0.15f;   // Lower = snappier, higher = floatier
    public Vector3 offset = new Vector3(0f, 12f, -10f); // Camera offset from target
    public bool rotateWithPlayers = false;   // Keep false so W is always North

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);

    [Header("Zoom (optional)")]
    public bool dynamicZoom = true;
    public float minDistance = 8f;    // Zoom-in limit (players close together)
    public float maxDistance = 18f;   // Zoom-out limit (players far apart)
    public float distanceMultiplier = 1.2f; // How aggressively to zoom out

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (playerA == null || playerB == null) return;

        // 1. Midpoint between both players (flattened to ground level)
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        midpoint.y = 0f; // ignore vertical bobbing

        // 2. Optionally zoom out based on how far apart players are
        Vector3 dynamicOffset = offset;
        if (dynamicZoom)
        {
            float dist = Vector3.Distance(
                new Vector3(playerA.position.x, 0, playerA.position.z),
                new Vector3(playerB.position.x, 0, playerB.position.z));

            float zoom = Mathf.Clamp(dist * distanceMultiplier, minDistance, maxDistance);
            // Preserve the angle, just scale the horizontal distance & height
            Vector3 flatOffset = new Vector3(offset.x, 0f, offset.z).normalized * zoom;
            dynamicOffset = new Vector3(flatOffset.x, offset.y, flatOffset.z);
        }

        // 3. Compute target position
        Vector3 targetPos = midpoint + dynamicOffset;

        // 4. Clamp to bounds if enabled
        if (useBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
            targetPos.z = Mathf.Clamp(targetPos.z, minBounds.y, maxBounds.y);
        }

        // 5. Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref velocity, followSmoothTime);

        // 6. FIXED rotation — camera always looks at the midpoint from the same angle
        //    This is what keeps W = North, ↑ = North, etc.
        transform.rotation = Quaternion.Euler(50f, 0f, 0f);
    }
}