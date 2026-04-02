using UnityEngine;
using TMPro;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager. Handles timer, collectible count, game over and win conditions.
/// Uses TextMeshPro for UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Timer")]
    public float levelTimeSeconds = 120f; // 2 minutes
    public TMP_Text timerText;
    public TMP_Text collectedText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverMessageText;

    int totalCollectibles = 0;
    int collectedCount = 0;
    float timeLeft;
    bool gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
       // Time.timeScale = 1.0f;
        timeLeft = levelTimeSeconds;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Count collectible objects in scene tagged "Collectible"
        var collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        totalCollectibles = collectibles.Length;

        UpdateCollectedUI();
    }

    void Update()
    {
        if (gameEnded) return;

        timeLeft -= Time.deltaTime;
        UpdateTimerUI();

        if (timeLeft <= 0f)
        {
            GameOver("Time's up");
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void RegisterCollectiblePickup()
    {
        collectedCount++;
        UpdateCollectedUI();

        if (collectedCount >= totalCollectibles)
        {
            WinGame();
        }
    }

    void UpdateCollectedUI()
    {
        if (collectedText != null)
            collectedText.text = $"Collected: {collectedCount}/{totalCollectibles}";
    }

    public void GameOver(string reason)
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 0f; // pause

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverMessageText != null)
                gameOverMessageText.text = "Game Over\n" + reason;
        }

        Debug.Log("Game Over: " + reason);
    }

    void WinGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverMessageText != null)
                gameOverMessageText.text = "You Win!\nAll cubes collected";
        }

        Debug.Log("You Win!");
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        // RESET GLOBAL GRAVITY
        Physics.gravity = Vector3.down * 9.81f;

        // OPTIONAL: reset your camera state if needed (yaw/pitch reset)
        CameraFollow cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.ResetCameraState();    // we'll create this function below
        }

        // RESET PLAYER ROTATION (so character faces up normally)
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.rotation = Quaternion.identity;
        }

        // FINALLY RELOAD SCENE
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(0);
    }
}
