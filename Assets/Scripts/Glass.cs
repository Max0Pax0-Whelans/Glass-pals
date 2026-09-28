using UnityEngine;

public class GlassCarryManager : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;

    [Header("Glass Anchoring")]
    [Tooltip("How fast the glass follows the players when they move. Lower = more 'lag'.")]
    public float glassFollowSpeed = 6f;
    [Tooltip("How fast the glass rotates to match the players' orientation.")]
    public float glassRotateSpeed = 8f;
    [Tooltip("Dead zone: player movements smaller than this won't move the glass.")]
    public float glassDeadZone = 0.02f;
    [Header("Glass")]
    public Transform glass;
    public float carryHeightOffset = 0.5f;
    public float forwardOffset = 0.3f;

    [Header("Slot Constraint")]
    [Tooltip("How tightly players are locked to their slot. Lower = softer.")]
    public float constraintStiffness = 20f;
    [Tooltip("Maximum distance a player can drift from their slot before being pulled back.")]
    public float maxSlotDistance = 0.4f;
    [Tooltip("Damping to prevent oscillation.")]
    public float constraintDamping = 12f;
    [Tooltip("Max speed the constraint can push the player. Prevents launching.")]
    public float maxConstraintSpeed = 8f;

    [Header("Break Detection")]
    public float maxPlayerDistance = 2.5f;
    public float maxTiltAngle = 40f;
    public float stressPerUnit = 10f;

    [Header("Stress Settings")]
    public float stress = 0f;
    public float stressDecay = 15f;
    public float stressLimit = 100f;

    [Header("Break Effects")]
    public GameObject shatterPrefab;
    public float shatterLifetime = 5f;
    public GameObject breakParticles;
    public AudioClip breakSound;

    [Header("Shard Physics")]
    public float momentumTransfer = 1f;
    public float explosionForce = 150f;
    public float explosionRadius = 3f;

    [Header("Slope Handling")]
    public float maxGlassTilt = 45f;
    public float rotationSmoothSpeed = 15f;

    private bool isBroken = false;
    public bool IsBroken => isBroken;
    public float StressNormalized => stress / stressLimit;
    public System.Action OnGlassBroken;

    private Vector3 previousGlassPos;
    private Vector3 estimatedGlassVelocity;
    private Quaternion targetGlassRotation;

    private const float SlotA = -1f;
    private const float SlotB = 1f;

    // Startup grace period so first-frame garbage doesn't launch players
    private int startupFrames = 0;
    private const int StartupGraceFrames = 3;

    void Start()
    {
        if (playerA == null || playerB == null || glass == null) return;
        SnapGlassToTarget();
        previousGlassPos = glass.position;
    }

    void SnapGlassToTarget()
    {
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        float baseY = (playerA.position.y + playerB.position.y) * 0.5f;
        midpoint.y = baseY + carryHeightOffset;

        Vector3 avgFacing = (playerA.forward + playerB.forward).normalized;
        avgFacing.y = 0f;
        if (avgFacing.sqrMagnitude < 0.001f) avgFacing = Vector3.forward;
        midpoint += avgFacing * forwardOffset;

        glass.position = midpoint;

        Vector3 gripA = playerA.position + Vector3.up * carryHeightOffset;
        Vector3 gripB = playerB.position + Vector3.up * carryHeightOffset;
        Vector3 direction = gripB - gripA;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Vector3 right = direction.normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            glass.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 90f, 0f);

            Vector3 scale = glass.localScale;
            scale.x = direction.magnitude;
            glass.localScale = scale;
        }
    }

    void LateUpdate()
    {
        if (isBroken || playerA == null || playerB == null || glass == null)
            return;

        if (Time.deltaTime > 0f)
            estimatedGlassVelocity = (glass.position - previousGlassPos) / Time.deltaTime;
        previousGlassPos = glass.position;

        AlignGlassBetweenPlayers();
        EvaluateStress();

        if (startupFrames < StartupGraceFrames)
            startupFrames++;
    }

    void AlignGlassBetweenPlayers()
    {
        // Target position
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        float baseY = (playerA.position.y + playerB.position.y) * 0.5f; // average
        midpoint.y = baseY + carryHeightOffset;

        Vector3 avgFacing = (playerA.forward + playerB.forward).normalized;
        avgFacing.y = 0f;
        if (avgFacing.sqrMagnitude < 0.001f) avgFacing = Vector3.forward;
        Vector3 targetPos = midpoint + avgFacing * forwardOffset;

        // Target rotation
        Vector3 gripA = playerA.position + Vector3.up * carryHeightOffset;
        Vector3 gripB = playerB.position + Vector3.up * carryHeightOffset;
        Vector3 direction = gripB - gripA;
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);
        float slopeAngle = Vector3.Angle(horizontalDir, direction);
        if (slopeAngle > maxGlassTilt)
        {
            float t = maxGlassTilt / slopeAngle;
            direction.y = Mathf.Lerp(0f, direction.y, t);
        }

        Vector3 right = direction.normalized;
        Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        targetGlassRotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 90f, 0f);

        // Move toward target with deadzone
        float distToTarget = Vector3.Distance(glass.position, targetPos);
        if (distToTarget > glassDeadZone)
        {
            glass.position = Vector3.Lerp(
                glass.position, targetPos, glassFollowSpeed * Time.deltaTime);
        }

        // Rotate toward target
        glass.rotation = Quaternion.Slerp(
            glass.rotation, targetGlassRotation, glassRotateSpeed * Time.deltaTime);

        // Scale
        float targetScale = direction.magnitude;
        Vector3 scale = glass.localScale;
        scale.x = Mathf.Lerp(scale.x, targetScale, glassFollowSpeed * Time.deltaTime);
        glass.localScale = scale;
    }

    void FixedUpdate()
    {
        if (isBroken || playerA == null || playerB == null || glass == null)
            return;

        // Wait a few frames so the glass is properly positioned
        if (startupFrames < StartupGraceFrames)
            return;

        ApplySlotConstraint(playerA, SlotA);
        ApplySlotConstraint(playerB, SlotB);
    }

    Vector3 GetSlotWorldPosition(float slotT)
    {
        float halfLength = glass.localScale.x * 0.5f;
        return glass.position + glass.right * (slotT * halfLength);
    }

    void ApplySlotConstraint(Transform player, float slotT)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 slotPos = GetSlotWorldPosition(slotT);
        Vector3 toSlot = slotPos - player.position;
        toSlot.y = 0f; // only constrain horizontally

        float dist = toSlot.magnitude;

        // Sanity check: if the slot is ridiculously far away, something's wrong.
        // Skip the constraint this frame instead of catapulting the player.
        if (dist > 10f)
        {
            // Teleport the player to the slot to resync (safety net)
            Vector3 safePos = new Vector3(slotPos.x, player.position.y, slotPos.z);
            rb.MovePosition(safePos);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // Don't do anything if we're already within the allowed radius
        if (dist <= maxSlotDistance)
            return;

        // Only correct the amount of overshoot (not the full distance)
        float overshoot = dist - maxSlotDistance;
        Vector3 pullDir = toSlot.normalized;

        // Compute desired corrective velocity
        Vector3 correctiveVel = pullDir * overshoot * constraintStiffness;

        // Clamp corrective velocity so we never launch the player
        if (correctiveVel.magnitude > maxConstraintSpeed)
            correctiveVel = correctiveVel.normalized * maxConstraintSpeed;

        // Blend it in rather than slamming it — much more stable
        Vector3 newVel = rb.linearVelocity + correctiveVel * Time.fixedDeltaTime;

        // Damping: also reduce any velocity that pushes away from the slot
        float outwardVel = Vector3.Dot(rb.linearVelocity, -pullDir);
        if (outwardVel > 0f)
            newVel += pullDir * outwardVel * constraintDamping * Time.fixedDeltaTime;

        // Preserve Y (gravity / jumping)
        newVel.y = rb.linearVelocity.y;

        rb.linearVelocity = newVel;
    }

    

    void EvaluateStress()
    {
        float distance = Vector3.Distance(
            new Vector3(playerA.position.x, 0, playerA.position.z),
            new Vector3(playerB.position.x, 0, playerB.position.z));

        float distStress = Mathf.Max(0f, distance - maxPlayerDistance) * stressPerUnit;
        float heightDiff = Mathf.Abs(playerA.position.y - playerB.position.y);
        float heightStress = Mathf.Max(0f, heightDiff - 1.5f) * stressPerUnit;

        float incoming = distStress + heightStress;
        if (incoming > 0f) stress += incoming * Time.deltaTime;
        else stress -= stressDecay * Time.deltaTime;

        stress = Mathf.Clamp(stress, 0f, stressLimit);

        if (stress >= stressLimit) BreakGlass();
    }

    void BreakGlass()
    {
        if (isBroken) return;
        isBroken = true;

        Vector3 breakPos = glass.position;
        Quaternion breakRot = glass.rotation;
        Vector3 breakScale = glass.localScale;

        Vector3 inheritedVelocity = estimatedGlassVelocity * momentumTransfer;
        inheritedVelocity.y = 0f;

        if (shatterPrefab != null)
        {
            GameObject shards = Instantiate(shatterPrefab, breakPos, breakRot);
            shards.transform.localScale = breakScale;

            foreach (Rigidbody shardRb in shards.GetComponentsInChildren<Rigidbody>())
            {
                shardRb.isKinematic = false;
                shardRb.useGravity = true;
                shardRb.linearVelocity = inheritedVelocity;
                shardRb.AddExplosionForce(explosionForce, breakPos, explosionRadius, 0.5f, ForceMode.Impulse);
            }

            if (shatterLifetime > 0f) Destroy(shards, shatterLifetime);
        }

        if (breakParticles != null)
        {
            GameObject fx = Instantiate(breakParticles, breakPos, breakRot);
            Destroy(fx, 3f);
        }

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, breakPos);

        OnGlassBroken?.Invoke();
        Destroy(glass.gameObject);
        glass = null;
    }

}