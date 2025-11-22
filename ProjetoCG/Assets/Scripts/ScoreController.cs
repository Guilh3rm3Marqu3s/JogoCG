using UnityEngine;
using UnityEngine.UI; // Required for Slider
using TMPro;
using StarterAssets;          // Required for Text Mesh Pro

public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance { get; private set; }

    public FirstPersonController playerController;

    [Header("Game Settings")]
    [Tooltip("Time in seconds for bar to fill naturally (10 mins = 600)")]
    public float MaxTimeSeconds = 600f; 
    [Tooltip("How much the bar goes down when interacting (0.1 = 10%)")]
    public float InteractionReduction = 0.1f;

    [Header("UI References")]
    public Slider CompletionBar;
    public TextMeshProUGUI ScoreText;
    public GameObject WinScreen; // Optional: Assign a panel to show when winning
    public TextMeshProUGUI WinScoreText;
    public GameObject LoseScreen; // Optional: Assign a panel to show when losing
    public TextMeshProUGUI LoseScoreText;

    // Internal State
    private float _currentCompletion = 0f;
    private int _totalObjectsInScene;
    private int _interactedCount = 0;
    private float _currentScore = 0;
    private float _lastInteractionTime;
    private bool _isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        // Initialize timers
        _lastInteractionTime = Time.time;
    }

    private void Start()
    {
        // Automatically count how many interactables are in the scene
        _totalObjectsInScene = FindObjectsOfType<InteractableTrash>().Length;
        Debug.Log($"Game Started. Total Objects to find: {_totalObjectsInScene}");
        
        UpdateUI();
    }

    private void Update()
    {
        if (_isGameOver) return;

        // 1. Fill the bar naturally over 10 minutes
        // We divide Time.deltaTime by MaxTimeSeconds to get the fraction per second
        _currentCompletion += Time.deltaTime / MaxTimeSeconds;

        // 2. Check for "Time Out" (Bar Full)
        if (_currentCompletion >= 1.0f)
        {
            _currentCompletion = 1.0f;
            EndGame(false); // Lose
        }

        UpdateUI();
    }

    public void RegisterInteraction()
    {
        if (_isGameOver) return;

        // --- SCORING LOGIC ---
        
        // Factor 1: Base points for the object
        float points = 100f;

        // Factor 2: Time between interactions (The faster, the better)
        float timeSinceLast = Time.time - _lastInteractionTime;
        // Example: If you find next object in 10s, you get bonus. If 60s, no bonus.
        float speedBonus = Mathf.Clamp(500f - (timeSinceLast * 10f), 0, 500f);
        
        _currentScore += points + speedBonus;

        // Update Logic
        _interactedCount++;
        _lastInteractionTime = Time.time; // Reset timer for next interaction

        // --- BAR MECHANIC ---
        // Reduce the bar (clamped so it doesn't go below 0)
        _currentCompletion = Mathf.Clamp01(_currentCompletion - InteractionReduction);

        Debug.Log($"Object {_interactedCount}/{_totalObjectsInScene} Collected. Score: {_currentScore}");

        // --- WIN CONDITION ---
        if (_interactedCount >= _totalObjectsInScene)
        {
            EndGame(true); // Win
        }
    }

    private void EndGame(bool playerWon)
    {
        _isGameOver = true;

        if (playerController != null)
        {
            playerController.LockControls = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float barEmptySpace = 1.0f - _currentCompletion;
            float timeBonus = barEmptySpace * 1000f;
            _currentScore += timeBonus;

        int finalScore = Mathf.FloorToInt(_currentScore);

        if (playerWon)
        {
            Debug.Log("YOU WIN! Final Score: " + finalScore);
            if(WinScreen) WinScreen.SetActive(true);
            if (WinScoreText) WinScoreText.text = "Final Score: " + finalScore;
        }
        else
        {
            Debug.Log("GAME OVER! Bar filled up.");
            if(LoseScreen) LoseScreen.SetActive(true);
            if (LoseScoreText) LoseScoreText.text = "Final Score: " + finalScore;
        }
    }

    private void UpdateUI()
    {
        if (CompletionBar) CompletionBar.value = _currentCompletion;
        if (ScoreText) ScoreText.text = $"Score: {Mathf.FloorToInt(_currentScore)}\nObjects: {_interactedCount}/{_totalObjectsInScene}";
    }
}