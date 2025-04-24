using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationUIManager : MonoBehaviour
{
    [Header("Control Buttons")]
    public Button pauseButton;
    public Button speedDownButton;
    public Button speedUpButton;
    public TextMeshProUGUI speedLabel;
    public Button hamburgerButton;

    [Header("Metrics Panel")]
    public GameObject metricsPanel;
    public TextMeshProUGUI daysText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI agentsText;
    public TextMeshProUGUI enemiesText;
    public TextMeshProUGUI foodText;

    [Header("Database References")]
    public TimeManager timeManager;
    public SpawnManager spawnManager;

    [Header("Metrics Panel Sizing")]
    public Vector2 collapsedSize = new Vector2(  40,  40);
    public Vector2 expandedSize  = new Vector2(200, 200);

    private RectTransform metricsRect;


    // internal
    private bool isPaused = false;
    private float speedStep = 0.5f;
    private float minSpeed = 0.1f;
    private float maxSpeed = 5f;

    void Start()
    {
        // Hook up clicks
        pauseButton.onClick.AddListener(TogglePause);
        speedDownButton.onClick.AddListener(DecreaseSpeed);
        speedUpButton.onClick.AddListener(IncreaseSpeed);
        hamburgerButton.onClick.AddListener(ToggleMetricsPanel);

        // Find managers if not assigned
        if (timeManager == null)     timeManager = FindObjectOfType<TimeManager>();
        if (spawnManager == null)    spawnManager = FindObjectOfType<SpawnManager>();

        // Init UI
        UpdateSpeedLabel();
        metricsPanel.SetActive(false);

        metricsRect = metricsPanel.GetComponent<RectTransform>();
        metricsRect.sizeDelta = collapsedSize;
    }

    void Update()
    {
        RefreshMetrics();
    }

    // ─── Controls ─────────────────────────────────────────

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pauseButton.GetComponentInChildren<TextMeshProUGUI>().text = isPaused ? "▶" : "⏸";
        UpdateSpeedLabel();
    }

    private void IncreaseSpeed()
    {
        if (isPaused) return;
        Time.timeScale = Mathf.Min(Time.timeScale + speedStep, maxSpeed);
        UpdateSpeedLabel();
    }

    private void DecreaseSpeed()
    {
        if (isPaused) return;
        Time.timeScale = Mathf.Max(Time.timeScale - speedStep, minSpeed);
        UpdateSpeedLabel();
    }

    private void UpdateSpeedLabel()
    {
        speedLabel.text = $"x{Time.timeScale:F1}";
    }

    private void ToggleMetricsPanel()
    {
        bool willShow = !metricsPanel.activeSelf;
        metricsPanel.SetActive(willShow);
        
        // resize the box:
        metricsRect.sizeDelta = willShow 
            ? expandedSize 
            : collapsedSize;
    }

    // ─── Metrics ──────────────────────────────────────────

    private void RefreshMetrics()
    {
        if (timeManager != null)
        {
            daysText.text = $"Days Passed: {timeManager.Days}";
            timeText.text = $"Time Passed: {timeManager.Hours:00}:{timeManager.Minutes:00}";
        }

        if (spawnManager != null)
        {
            // these lists must be public or exposed via a property
            agentsText.text  = $"Number of Agents: {spawnManager.AgentCount}";
            enemiesText.text = $"Number of Enemies: {spawnManager.EnemyCount}";
            foodText.text    = $"Number of Food: {spawnManager.FoodCount}";

        }
    }
}
