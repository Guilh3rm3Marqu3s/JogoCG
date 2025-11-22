using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using StarterAssets; 

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
    public GameObject WinScreen; 
    public TextMeshProUGUI WinScoreText;
    public GameObject LoseScreen; 
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
        
        // IMPORTANTE: Garante que o jogo não comece pausado se você reiniciou a cena
        Time.timeScale = 1f;

        _lastInteractionTime = Time.time;
    }

    private void Start()
    {
        _totalObjectsInScene = FindObjectsOfType<InteractableTrash>().Length;
        UpdateUI();
    }

    private void Update()
    {
        if (_isGameOver) return;

        // Enche a barra com o tempo
        _currentCompletion += Time.deltaTime / MaxTimeSeconds;

        // Se a barra encher, perde por tempo esgotado
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

        float points = 100f;
        float timeSinceLast = Time.time - _lastInteractionTime;
        float speedBonus = Mathf.Clamp(500f - (timeSinceLast * 10f), 0, 500f);
        
        _currentScore += points + speedBonus;

        _interactedCount++;
        _lastInteractionTime = Time.time; 

        _currentCompletion = Mathf.Clamp01(_currentCompletion - InteractionReduction);

        if (_interactedCount >= _totalObjectsInScene)
        {
            EndGame(true); // Win
        }
    }

    // --- NOVA FUNÇÃO: Chamada pelo PlayerHealth quando a vida zera ---
    public void TriggerDeath()
    {
        if (_isGameOver) return; // Evita chamar duas vezes
        Debug.Log("Game Over por Morte!");
        EndGame(false);
    }

    private void EndGame(bool playerWon)
    {
        _isGameOver = true;

        // 1. TRAVA O JOGO (Física, Movimento, Animações)
        Time.timeScale = 0f;

        // 2. Destrava o Mouse
        if (playerController != null)
        {
            // Tenta travar o input do script (opcional já que o timeScale faz a maior parte)
            playerController.enabled = false; 
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Calcula bonus final se ganhou
        if (playerWon)
        {
            float barEmptySpace = 1.0f - _currentCompletion;
            float timeBonus = barEmptySpace * 1000f;
            _currentScore += timeBonus;
        }

        int finalScore = Mathf.FloorToInt(_currentScore);

        // 3. Exibe as telas
        if (playerWon)
        {
            if(WinScreen) WinScreen.SetActive(true);
            if (WinScoreText) WinScoreText.text = "YOU ESCAPED!\nFinal Score: " + finalScore;
        }
        else
        {
            if(LoseScreen) LoseScreen.SetActive(true);
            // Aqui você pode customizar a mensagem dependendo se foi Tempo ou Morte,
            // mas genericamente "Game Over" serve para os dois.
            if (LoseScoreText) LoseScoreText.text = "GAME OVER\nFinal Score: " + finalScore;
        }
    }

    private void UpdateUI()
    {
        if (CompletionBar) CompletionBar.value = _currentCompletion;
        if (ScoreText) ScoreText.text = $"Score: {Mathf.FloorToInt(_currentScore)}\nObjects: {_interactedCount}/{_totalObjectsInScene}";
    }
}