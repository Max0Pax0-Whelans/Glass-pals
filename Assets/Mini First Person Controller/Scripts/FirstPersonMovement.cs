using System.Collections.Generic;
using UnityEngine;

public enum InputScheme { WASD, ArrowKeys }

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Glass Height Input")]
    [Tooltip("Key to raise the glass.")]
    public KeyCode raiseKey = KeyCode.Q;
    [Tooltip("Key to lower the glass.")]
    public KeyCode lowerKey = KeyCode.E;
    [Tooltip("Reference to the glass manager.")]
    public GlassCarryManager glassManager;
    [Header("Input")]
    public InputScheme inputScheme = InputScheme.WASD;

    [Header("Movement")]
    public float speed = 5f;
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private Rigidbody rb;
    private Vector2 inputVector;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // --- Existing movement input ---
        float h = 0f, v = 0f;

        if (inputScheme == InputScheme.WASD)
        {
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
        }

        IsRunning = canRun && Input.GetKey(runningKey);

        inputVector = new Vector2(h, v);
        if (inputVector.sqrMagnitude > 1f) inputVector.Normalize();

        // --- ADD THIS: Raise/lower glass input ---
        if (glassManager != null)
        {
            float raiseInput = 0f;
            if (Input.GetKey(raiseKey)) raiseInput += 1f;
            if (Input.GetKey(lowerKey)) raiseInput -= 1f;

            if (inputScheme == InputScheme.WASD)
                glassManager.SetRaiseInputA(raiseInput);
            else
                glassManager.SetRaiseInputB(raiseInput);
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
            targetSpeed = speedOverrides[speedOverrides.Count - 1]();

        rb.linearVelocity = new Vector3(
            inputVector.x * targetSpeed,
            rb.linearVelocity.y,
            inputVector.y * targetSpeed);

        // Face movement direction
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = new Vector3(inputVector.x, 0f, inputVector.y);
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, 720f * Time.fixedDeltaTime));
        }
    }
}