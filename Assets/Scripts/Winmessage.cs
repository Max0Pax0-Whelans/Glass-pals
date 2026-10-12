using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class WinTrigger : MonoBehaviour
{
    [Header("Win Detection")]
    [Tooltip("Which players/objects must reach the goal for a win.")]
    public Transform playerA;
    public Transform playerB;

    [Tooltip("If true, both players must be inside the trigger to win.")]
    public bool requireBothPlayers = true;

    [Tooltip("If true, the glass must also be inside the trigger to win.")]
    public bool requireGlass = false;

    [Tooltip("Reference to the glass (needed if requireGlass is true).")]
    public Transform glass;

    [Header("Win UI")]
    [Tooltip("The panel to show on win.")]
    public GameObject winPanel;

    [Tooltip("Optional: text to display on the panel.")]
    public TextMeshProUGUI winText;

    [Tooltip("Text shown on the win panel.")]
    public string winMessage = "YOU WIN!";

    [Header("Buttons (Optional)")]
    public Button nextLevelButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Next Level")]
    [Tooltip("Scene name to load when Next Level is clicked. Leave empty to hide the button.")]
    public string nextLevelSceneName = "";

    [Tooltip("Scene name of the main menu. Leave empty to hide the button.")]
    public string mainMenuSceneName = "TitleScreen";

    [Header("Effects (Optional)")]
    public AudioClip winSound;
    public GameObject winParticles;

    [Header("Settings")]
    [Tooltip("Pause the game (freeze physics) when the win screen shows.")]
    public bool pauseOnWin = true;

    private bool hasWon = false;
    private int playersInside = 0;
    private bool glassInside = false;

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);

        // Ensure this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[WinTrigger] No collider on {name}. Adding a BoxCollider set to trigger.");
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[WinTrigger] Collider on {name} is not a trigger. Setting to trigger.");
            col.isTrigger = true;
        }

        // Wire up buttons
        if (nextLevelButton != null)
        {
            if (string.IsNullOrEmpty(nextLevelSceneName))
                nextLevelButton.gameObject.SetActive(false);
            else
                nextLevelButton.onClick.AddListener(() => SceneManager.LoadScene(nextLevelSceneName));
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(() =>
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        if (mainMenuButton != null)
        {
            if (string.IsNullOrEmpty(mainMenuSceneName))
                mainMenuButton.gameObject.SetActive(false);
            else
                mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasWon) return;

        // Track player entry
        if (other.transform == playerA || other.transform == playerB)
        {
            playersInside++;
            CheckWin();
        }

        // Track glass entry
        if (requireGlass && other.transform == glass)
        {
            glassInside = true;
            CheckWin();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (hasWon) return;

        if (other.transform == playerA || other.transform == playerB)
        {
            playersInside = Mathf.Max(0, playersInside - 1);
        }

        if (requireGlass && other.transform == glass)
        {
            glassInside = false;
        }
    }

    void CheckWin()
    {
        if (hasWon) return;

        bool playersOK = requireBothPlayers ? playersInside >= 2 : playersInside >= 1;
        bool glassOK = !requireGlass || glassInside;

        if (playersOK && glassOK)
        {
            TriggerWin();
        }
    }

    void TriggerWin()
    {
        hasWon = true;

        // Show the win panel
        if (winPanel != null) winPanel.SetActive(true);

        // Set win text
        if (winText != null) winText.text = winMessage;

        // Play sound
        if (winSound != null)
            AudioSource.PlayClipAtPoint(winSound, transform.position);

        // Spawn particles
        if (winParticles != null)
        {
            GameObject fx = Instantiate(winParticles, transform.position, Quaternion.identity);
            Destroy(fx, 5f);
        }

        // Pause the game
        if (pauseOnWin)
            Time.timeScale = 0f;

        Debug.Log("WIN!");
    }

    // Call from a button if you want to unpause before loading a scene
    public void UnpauseGame()
    {
        Time.timeScale = 1f;
    }
}