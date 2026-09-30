using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("The root panel that gets shown/hidden.")]
    public GameObject panel;

    [Header("Buttons")]
    public Button restartButton;
    public Button quitButton;

    [Header("Optional")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;

    void Awake()
    {
        // Auto-hide on start
        if (panel != null) panel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);

        if (audioSource != null && gameOverSound != null)
            audioSource.PlayOneShot(gameOverSound);

        // Unlock cursor if needed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optional: pause the game
        // Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnRestartClicked()
    {
        // Optional: unpause before restarting
        // Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}