using UnityEngine;

public class SwingingObject : MonoBehaviour
{
    [Header("Swing Setup")]
    [Tooltip("The pivot point the object swings from. If null, creates one above this object.")]
    public Transform pivot;

    [Tooltip("Distance from the pivot to this object (the rope/chain length).")]
    public float swingLength = 5f;

    [Header("Initial Swing")]
    [Tooltip("Initial angle (degrees) from vertical. 90 = horizontal start.")]
    public float startAngle = 60f;

    [Tooltip("Initial push force. 0 = swings gently from gravity, higher = more powerful.")]
    public float initialPushForce = 5f;

    [Tooltip("Direction of the initial push (horizontal axis). -1 = left, 1 = right.")]
    public float initialPushDirection = 1f;

    [Header("Swing Limits")]
    [Tooltip("If true, uses a HingeJoint for physics-based swinging. If false, uses math-based swinging.")]
    public bool usePhysicsSwing = true;

    [Header("Setup")]
    [Tooltip("The Rigidbody of the swinging object. Auto-added if missing.")]
    public Rigidbody rb;

    [Tooltip("Mass of the swinging object.")]
    public float mass = 20f;

    [Header("Hazard")]
    [Tooltip("If true, this object can shatter the glass on contact.")]
    public bool breaksGlass = true;

    [Tooltip("Layers that count as hazards for the glass.")]
    public LayerMask hazardLayers;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.red;

    private HingeJoint hinge;

    void Awake()
    {
        // Get or add a Rigidbody
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.useGravity = true;

        // If no pivot is assigned, create one above this object
        if (pivot == null)
        {
            GameObject pivotGO = new GameObject(name + "_Pivot");
            pivotGO.transform.SetParent(transform.parent);
            pivotGO.transform.position = transform.position + Vector3.up * swingLength;
            pivot = pivotGO.transform;
        }

        // Position this object at the correct distance from the pivot
        Vector3 toObject = (transform.position - pivot.position).normalized;
        if (toObject.sqrMagnitude < 0.001f)
            toObject = Vector3.down;
        transform.position = pivot.position + toObject * swingLength;
    }

    void Start()
    {
        if (usePhysicsSwing)
        {
            SetupHingeJoint();
            ApplyInitialPush();
        }
    }

    void SetupHingeJoint()
    {
        // Attach the hinge to a kinematic anchor at the pivot
        hinge = gameObject.AddComponent<HingeJoint>();
        hinge.autoConfigureConnectedAnchor = false;

        // Anchor is a point at the pivot
        hinge.anchor = transform.InverseTransformPoint(pivot.position);
        hinge.connectedAnchor = pivot.position;

        // Set the hinge axis (perpendicular to the swing plane)
        hinge.axis = Vector3.forward;

        // Optional: limit the swing angle
        JointLimits limits = hinge.limits;
        limits.min = -85f;
        limits.max = 85f;
        hinge.limits = limits;
        hinge.useLimits = true;

        // Optional: add some bounce/damping
        JointSpring spring = hinge.spring;
        spring.spring = 0f;   // no spring, just free swing
        spring.damper = 0.5f; // slight damping so it slows down over time
        hinge.spring = spring;
        hinge.useSpring = true;
    }

    void ApplyInitialPush()
    {
        if (Mathf.Approximately(initialPushForce, 0f)) return;

        Vector3 pushDir = Vector3.right * Mathf.Sign(initialPushDirection);
        rb.AddForce(pushDir * initialPushForce, ForceMode.Impulse);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3 pivotPos = pivot != null
            ? pivot.position
            : transform.position + Vector3.up * swingLength;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(pivotPos, 0.15f);
        Gizmos.DrawLine(pivotPos, transform.position);
    }
}