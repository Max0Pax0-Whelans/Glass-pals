using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerArm : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's body transform.")]
    public Transform playerBody;

    [Tooltip("The glass carry manager.")]
    public GlassCarryManager glassManager;

    [Tooltip("Which end of the glass this arm grips.")]
    public GlassCarryManager.PlayerSide side = GlassCarryManager.PlayerSide.A;

    [Header("Anchor Points")]
    [Tooltip("Offset from the player's center where the arm starts.")]
    public Vector3 shoulderOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Small offset from the glass grip point to avoid clipping through the glass.")]
    public Vector3 gripOffset = Vector3.zero;

    [Header("Line Renderer Settings")]
    [Tooltip("Thickness of the arm at the shoulder.")]
    public float startWidth = 0.15f;

    [Tooltip("Thickness of the arm at the hand.")]
    public float endWidth = 0.12f;

    [Tooltip("Segments for a slight curve. 2 = straight line.")]
    public int segments = 8;

    [Tooltip("How much the arm sags (0 = straight, higher = more droop).")]
    public float sagAmount = 0f;

    [Header("Optional Hand")]
    [Tooltip("Optional small mesh that sits at the grip point.")]
    public Transform handMesh;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        line.useWorldSpace = true;
    }

    void LateUpdate()
    {
        bool valid = glassManager != null
                     && glassManager.glass != null
                     && playerBody != null;

        line.enabled = valid;
        if (handMesh != null) handMesh.gameObject.SetActive(valid);
        if (!valid) return;

        // --- Compute anchor points ---
        Vector3 shoulder = playerBody.position
                         + playerBody.TransformDirection(shoulderOffset);

        Vector3 grip = glassManager.GetGripPointForSide(side) + gripOffset;

        // --- Draw the line with optional sag ---
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(shoulder, grip, t);

            // Parabolic sag — droops in the middle
            float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
            pos.y -= sag;

            line.SetPosition(i, pos);
        }

        // --- Taper width along the line ---
        line.startWidth = startWidth;
        line.endWidth = endWidth;

        // --- Optional hand mesh at the grip point ---
        if (handMesh != null)
        {
            handMesh.position = grip;
            handMesh.rotation = glassManager.glass.rotation;
        }
    }

    // Optional: accessor for other scripts
    public Vector3 GetGripWorldPosition()
    {
        if (glassManager == null) return transform.position;
        return glassManager.GetGripPointForSide(side);
    }
}