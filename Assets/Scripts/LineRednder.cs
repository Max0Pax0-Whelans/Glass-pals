using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeRenderer : MonoBehaviour
{
    [Tooltip("The pivot at the top.")]
    public Transform pivot;

    [Tooltip("The swinging object at the bottom.")]
    public Transform swingingObject;

    [Tooltip("Number of segments for a slight curve.")]
    public int segments = 10;

    [Tooltip("How much the rope sags (0 = straight, higher = more sag).")]
    public float sagAmount = 0.3f;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
    }

    void LateUpdate()
    {
        if (pivot == null || swingingObject == null) return;

        Vector3 top = pivot.position;
        Vector3 bottom = swingingObject.position;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            // Linear interpolation
            Vector3 pos = Vector3.Lerp(top, bottom, t);

            // Add sag (parabola shape)
            float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
            pos.y -= sag;

            line.SetPosition(i, pos);
        }
    }
}