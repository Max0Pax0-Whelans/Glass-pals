using UnityEngine;

public class GlassCarryManager : MonoBehaviour
{
    [Header("Glass Height Control")]
    [Tooltip("How fast the glass moves up/down when players press the raise/lower keys.")]
    public float glassRaiseSpeed = 1.5f;
    [Tooltip("How far up the glass can go above its base height.")]
    public float maxRaiseHeight = 1.5f;
    [Tooltip("How far down the glass can go below its base height.")]
    public float maxLowerHeight = 0.8f;
    [Tooltip("How quickly the glass returns to base height when nobody is pressing.")]
    public float heightReturnSpeed = 2f;
    private float currentHeightOffset = 0f;
    private float raiseInputA = 0f;
    private float raiseInputB = 0f;
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;

    [Header("Glass")]
    public Transform glass;
    public float carryHeightOffset = 1.1f;
    public float forwardOffset = 0.3f;

    [Header("Slot Constraint")]
    public float constraintStiffness = 20f;
    public float maxSlotDistance = 0.4f;
    public float constraintDamping = 12f;
    public float maxConstraintSpeed = 8f;

    [Header("Break Detection")]
    public float maxPlayerDistance = 2.5f;
    public float maxTiltAngle = 40f;
    public float stressPerUnit = 10f;

    [Header("Stress Settings")]
    public float stress = 0f;
    public float stressDecay = 15f;
    public float stressLimit = 100f;

    [Header("Glass Anchoring")]
    public float glassFollowSpeed = 6f;
    public float glassRotateSpeed = 8f;
    public float glassDeadZone = 0.02f;

    [Header("Slope Handling")]
    public float maxGlassTilt = 45f;

    [Header("Break Effects")]
    public GameObject shatterPrefab;
    public float shatterLifetime = 5f;
    public GameObject breakParticles;
    public AudioClip breakSound;

    [Header("Shard Physics")]
    public float momentumTransfer = 1f;
    public float explosionForce = 300f;
    public float explosionRadius = 3f;
    public float explosionUpwardModifier = 0.5f;

    [Header("Hazard Detection")]
    public LayerMask hazardLayers;
    [Tooltip("Radius used for the swept sphere check. Keep small (0.05–0.2).")]
    public float hazardCastRadius = 0.1f;

    private bool isBroken = false;
    private bool isBreaking = false;   // prevents re-entry

    public bool IsBroken => isBroken;
    public float StressNormalized => stress / Mathf.Max(0.0001f, stressLimit);
    public System.Action OnGlassBroken;

    private Vector3 previousGlassPos;
    private Vector3 estimatedGlassVelocity;
    private Quaternion targetGlassRotation;

    private const float SlotA = -1f;
    private const float SlotB = 1f;

    private int startupFrames = 0;
    private const int StartupGraceFrames = 3;

    /// <summary>Player A calls this every frame with their raise/lower input.</summary>
    public void SetRaiseInputA(float input) { raiseInputA = Mathf.Clamp(input, -1f, 1f); }

    /// <summary>Player B calls this every frame with their raise/lower input.</summary>
    public void SetRaiseInputB(float input) { raiseInputB = Mathf.Clamp(input, -1f, 1f); }

    void UpdateHeightControl()
    {
        // Option B: summed input, clamped so one player can't exceed max speed
        float combined = raiseInputA + raiseInputB;
        combined = Mathf.Clamp(combined, -1f, 1f);

        if (Mathf.Abs(combined) > 0.01f)
        {
            currentHeightOffset += combined * glassRaiseSpeed * Time.deltaTime;
        }
        else
        {
            // Nobody pressing — drift back to base height
            currentHeightOffset = Mathf.MoveTowards(
                currentHeightOffset, 0f, heightReturnSpeed * Time.deltaTime);
        }

        currentHeightOffset = Mathf.Clamp(currentHeightOffset, -maxLowerHeight, maxRaiseHeight);
    }


    void Start()
    {
        if (playerA == null || playerB == null || glass == null) return;
        SnapGlassToTarget();
        previousGlassPos = glass.position;
    }

    void LateUpdate()
    {
        if (isBroken || playerA == null || playerB == null || glass == null)
            return;

        UpdateHeightControl();   // <-- ADD THIS LINE

        if (Time.deltaTime > 0f)
            estimatedGlassVelocity = (glass.position - previousGlassPos) / Time.deltaTime;

        Vector3 positionBeforeMove = glass.position;
        AlignGlassBetweenPlayers();
        CheckHazardsAlongPath(positionBeforeMove, glass.position);
        CheckHazardsAtPoint(glass.position);

        if (isBroken) return;

        previousGlassPos = glass.position;

        EvaluateStress();

        if (startupFrames < StartupGraceFrames)
            startupFrames++;
    }

    // Predicts where the glass will be next frame (based on the target)
    Vector3 ComputeNextGlassPosition()
    {
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        float baseY = (playerA.position.y + playerB.position.y) * 0.5f;
        midpoint.y = baseY + carryHeightOffset;

        Vector3 avgFacing = (playerA.forward + playerB.forward).normalized;
        avgFacing.y = 0f;
        if (avgFacing.sqrMagnitude < 0.001f) avgFacing = Vector3.forward;
        return midpoint + avgFacing * forwardOffset;
    }

    void FixedUpdate()
    {
        if (isBroken || isBreaking) return;
        if (playerA == null || playerB == null || glass == null) return;
        if (startupFrames < StartupGraceFrames) return;

        ApplySlotConstraint(playerA, SlotA);
        ApplySlotConstraint(playerB, SlotB);
    }

    // ---------- Hazard detection ----------
    void CheckHazardsAlongPath(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f) return;

        float halfThickness = Mathf.Max(glass.localScale.y, glass.localScale.z) * 0.5f;
        float radius = Mathf.Max(halfThickness, 0.15f);

        Vector3 direction = delta.normalized;
        float castDistance = distance + radius * 2f;

        RaycastHit[] hits = Physics.SphereCastAll(
            from, radius, direction, castDistance,
            hazardLayers, QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.gameObject == glass.gameObject) continue;

            Vector3 impactDirection = (glass.position - hit.point).normalized;
            float impactStrength = estimatedGlassVelocity.magnitude + 2f;

            BreakGlass(hit.point, impactDirection, impactStrength);
            return;
        }
    }

    void CheckHazardsAtPoint(Vector3 position)
    {
        float halfLength = glass.localScale.x * 0.5f;
        float halfThickness = Mathf.Max(glass.localScale.y, glass.localScale.z) * 0.5f;
        Vector3 halfExtents = new Vector3(halfLength, halfThickness, halfThickness);

        Collider[] hits = Physics.OverlapBox(
            position, halfExtents, glass.rotation,
            hazardLayers, QueryTriggerInteraction.Collide);

        foreach (Collider col in hits)
        {
            if (col == null) continue;
            if (col.gameObject == glass.gameObject) continue;

            Vector3 hitPoint = col.ClosestPoint(position);
            Vector3 impactDir = (position - hitPoint).normalized;
            BreakGlass(hitPoint, impactDir, 2f);
            return;
        }
    }

    bool IsShard(GameObject obj)
    {
        // Check the object and its parents for a "Shard" tag or a component
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag("Shard")) return true;
            // Also detect the shatter prefab root if it's still in the process of spawning
            if (shatterPrefab != null && t.name.StartsWith(shatterPrefab.name)) return true;
            t = t.parent;
        }
        return false;
    }

    // ---------- Slot constraint ----------
    Vector3 GetSlotWorldPosition(float slotT)
    {
        if (glass == null) return Vector3.zero;
        float halfLength = glass.localScale.x * 0.5f;
        return glass.position + glass.right * (slotT * halfLength);
    }

    void ApplySlotConstraint(Transform player, float slotT)
    {
        if (player == null || glass == null) return;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 slotPos = GetSlotWorldPosition(slotT);
        Vector3 toSlot = slotPos - player.position;
        toSlot.y = 0f;

        float dist = toSlot.magnitude;

        if (dist > 10f)
        {
            Vector3 safePos = new Vector3(slotPos.x, player.position.y, slotPos.z);
            rb.MovePosition(safePos);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (dist <= maxSlotDistance) return;

        float overshoot = dist - maxSlotDistance;
        Vector3 pullDir = toSlot / dist;

        Vector3 correctiveVel = pullDir * overshoot * constraintStiffness;
        if (correctiveVel.magnitude > maxConstraintSpeed)
            correctiveVel = correctiveVel.normalized * maxConstraintSpeed;

        Vector3 newVel = rb.linearVelocity + correctiveVel * Time.fixedDeltaTime;

        float outwardVel = Vector3.Dot(rb.linearVelocity, -pullDir);
        if (outwardVel > 0f)
            newVel += pullDir * outwardVel * constraintDamping * Time.fixedDeltaTime;

        newVel.y = rb.linearVelocity.y;
        rb.linearVelocity = newVel;
    }

    // ---------- Glass alignment ----------
    void SnapGlassToTarget()
    {
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        float baseY = (playerA.position.y + playerB.position.y) * 0.5f;
        midpoint.y = baseY + carryHeightOffset + currentHeightOffset;

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

    void AlignGlassBetweenPlayers()
    {
        Vector3 midpoint = (playerA.position + playerB.position) * 0.5f;
        float baseY = (playerA.position.y + playerB.position.y) * 0.5f;
        midpoint.y = baseY + carryHeightOffset + currentHeightOffset;

        Vector3 avgFacing = (playerA.forward + playerB.forward).normalized;
        avgFacing.y = 0f;
        if (avgFacing.sqrMagnitude < 0.001f) avgFacing = Vector3.forward;
        Vector3 targetPos = midpoint + avgFacing * forwardOffset;

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

        float distToTarget = Vector3.Distance(glass.position, targetPos);
        if (distToTarget > glassDeadZone)
            glass.position = Vector3.Lerp(glass.position, targetPos, glassFollowSpeed * Time.deltaTime);

        glass.rotation = Quaternion.Slerp(glass.rotation, targetGlassRotation, glassRotateSpeed * Time.deltaTime);

        float targetScale = direction.magnitude;
        Vector3 scale = glass.localScale;
        scale.x = Mathf.Lerp(scale.x, targetScale, glassFollowSpeed * Time.deltaTime);
        glass.localScale = scale;
    }

    // ---------- Stress ----------
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

        if (stress >= stressLimit)
        {
            Vector3 center = glass != null ? glass.position : Vector3.zero;
            BreakGlass(center, Vector3.zero, 0f);
        }
    }

    // ---------- Public API ----------
    public void NotifyGlassDestroyed()
    {
        if (isBroken || isBreaking) return;
        isBroken = true;
        glass = null;
        OnGlassBroken?.Invoke();
    }

    // The one true break method
    public void BreakGlass(Vector3 hitPoint, Vector3 impactDirection, float impactStrength)
    {
        // Re-entry guard: set BOTH flags immediately
        if (isBroken || isBreaking) return;
        isBreaking = true;
        isBroken = true;

        Vector3 breakPos = glass != null ? glass.position : hitPoint;
        Quaternion breakRot = glass != null ? glass.rotation : Quaternion.identity;
        Vector3 breakScale = glass != null ? glass.localScale : Vector3.one;

        Vector3 inheritedVelocity = estimatedGlassVelocity * momentumTransfer;
        inheritedVelocity.y = 0f;

        if (shatterPrefab != null)
        {
            GameObject shards = Instantiate(shatterPrefab, breakPos, breakRot);
            shards.transform.localScale = breakScale;

            // Tag the root so hazard checks ignore shards
            shards.tag = "Shard";
            foreach (Transform child in shards.GetComponentsInChildren<Transform>())
                child.gameObject.tag = "Shard";

            foreach (Rigidbody shardRb in shards.GetComponentsInChildren<Rigidbody>())
            {
                shardRb.isKinematic = false;
                shardRb.useGravity = true;
                shardRb.linearVelocity = inheritedVelocity;

                Vector3 explosionCenter = hitPoint != Vector3.zero ? hitPoint : breakPos;
                shardRb.AddExplosionForce(
                    explosionForce,
                    explosionCenter,
                    explosionRadius,
                    explosionUpwardModifier,
                    ForceMode.Impulse);
            }

            if (shatterLifetime > 0f)
                Destroy(shards, shatterLifetime);
        }

        if (breakParticles != null)
        {
            Vector3 fxPos = hitPoint != Vector3.zero ? hitPoint : breakPos;
            GameObject fx = Instantiate(breakParticles, fxPos, breakRot);
            Destroy(fx, 3f);
        }

        if (breakSound != null)
        {
            Vector3 soundPos = hitPoint != Vector3.zero ? hitPoint : breakPos;
            AudioSource.PlayClipAtPoint(breakSound, soundPos);
        }

        OnGlassBroken?.Invoke();

        if (glass != null) Destroy(glass.gameObject);
        glass = null;

        isBreaking = false;
    }
}