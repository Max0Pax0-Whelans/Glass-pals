using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GlassCarryManager glassManager;
    public GameOverUI gameOverUI;

    [Header("Restart Settings")]
    [Tooltip("Delay after the glass breaks before showing the game over screen.")]
    public float gameOverDelay = 1.5f;

    [Header("Player Spawn (fallback)")]
    public Transform defaultSpawnPoint;
    public Transform playerA;
    public Transform playerB;

    // The current respawn point (updated by checkpoints)
    public Vector3 CurrentSpawnPosition { get; private set; }
    public Quaternion CurrentSpawnRotation { get; private set; }

    private bool isGameOver = false;

    void Awake()
    {
        // Singleton pattern — ensures only one GameManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Set initial spawn from the default spawn point
        if (defaultSpawnPoint != null)
        {
            CurrentSpawnPosition = defaultSpawnPoint.position;
            CurrentSpawnRotation = defaultSpawnPoint.rotation;
        }
        else
        {
            CurrentSpawnPosition = playerA != null ? playerA.position : Vector3.zero;
            CurrentSpawnRotation = Quaternion.identity;
        }

        // Subscribe to the glass break event
        if (glassManager != null)
            glassManager.OnGlassBroken += HandleGlassBroken;

        if (gameOverUI != null)
            gameOverUI.Hide();
    }

    void OnDestroy()
    {
        if (glassManager != null)
            glassManager.OnGlassBroken -= HandleGlassBroken;
    }

    void HandleGlassBroken()
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(ShowGameOverAfterDelay());
    }

    IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (gameOverUI != null)
            gameOverUI.Show();
    }

    // Called by the Restart button
    public void RestartLevel()
    {
        // Option A: reload the whole scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Option B (if you want to keep checkpoints across restarts):
        // StartCoroutine(RespawnPlayersOnly());
    }

    // Optional alternative — respawn just the players without reloading
    IEnumerator RespawnPlayersOnly()
    {
        yield return null; // let one frame pass

        Rigidbody rbA = playerA.GetComponent<Rigidbody>();
        Rigidbody rbB = playerB.GetComponent<Rigidbody>();

        rbA.position = CurrentSpawnPosition;
        rbB.position = CurrentSpawnPosition;
        rbA.rotation = CurrentSpawnRotation;
        rbB.rotation = CurrentSpawnRotation;

        rbA.linearVelocity = Vector3.zero;
        rbB.linearVelocity = Vector3.zero;

        isGameOver = false;
    }

    // Called by checkpoints
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        CurrentSpawnPosition = position;
        CurrentSpawnRotation = rotation;
        Debug.Log($"Checkpoint set to {position}");
    }
}