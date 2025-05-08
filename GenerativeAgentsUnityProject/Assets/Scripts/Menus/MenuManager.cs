using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Menus & Environment")]
    public GameObject mainMenu;
    public GameObject settingsMenu;
    public GameObject aboutUsMenu;
    public GameObject contactUsMenu;
    public GameObject environmentContainer;

    [Header("Spawn Manager")]
    public SpawnManager spawnManager;

    [Header("Simulation Size Inputs")]
    public TMP_InputField agentsInput;
    public TMP_InputField enemiesInput;
    public TMP_InputField foodInput;

    [Header("Play Button (assign in Inspector)")]
    public Button playButton;

    private int selectedAgents;
    private int selectedEnemies;
    private int selectedFood;

    void Start()
    {
        // sanity checks
        if (spawnManager == null) Debug.LogError($"[{name}] SpawnManager not assigned!");
        if (agentsInput==null||enemiesInput==null||foodInput==null)
            Debug.LogError($"[{name}] One of the input fields is missing!");
        if (mainMenu==null||environmentContainer==null)
            Debug.LogError($"[{name}] One of the menu GameObjects is missing!");
        if (playButton == null)
            Debug.LogError($"[{name}] Play Button not assigned in the Inspector!");

        // populate the fields from your spawnManager defaults
        if (spawnManager != null)
        {
            agentsInput.text  = spawnManager.maxAgents.ToString();
            enemiesInput.text = spawnManager.maxEnemies.ToString();
            foodInput.text    = spawnManager.maxFood.ToString();
        }

        agentsInput.onEndEdit  .AddListener(OnAgentsInputChanged);
        enemiesInput.onEndEdit .AddListener(OnEnemiesInputChanged);
        foodInput.onEndEdit    .AddListener(OnFoodInputChanged);

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                Debug.Log("▶️ StartEnvironment() clicked");
                StartEnvironment();
            });
        }

        // prime our internal values
        OnAgentsInputChanged(agentsInput.text);
        OnEnemiesInputChanged(enemiesInput.text);
        OnFoodInputChanged(foodInput.text);

        // pause time until we hit Play
        Time.timeScale = 0f;

        OpenMainMenu();
    }

    private void OnAgentsInputChanged(string txt)
    {
        if (int.TryParse(txt, out var v) && v >= 0)
            selectedAgents = v;
        else
            agentsInput.text = selectedAgents.ToString();
    }

    private void OnEnemiesInputChanged(string txt)
    {
        if (int.TryParse(txt, out var v) && v >= 0)
            selectedEnemies = v;
        else
            enemiesInput.text = selectedEnemies.ToString();
    }

    private void OnFoodInputChanged(string txt)
    {
        if (int.TryParse(txt, out var v) && v >= 0)
            selectedFood = v;
        else
            foodInput.text = selectedFood.ToString();
    }

    public void StartEnvironment()
    {
        if (spawnManager == null) return;

        spawnManager.maxAgents  = selectedAgents;
        spawnManager.maxEnemies = selectedEnemies;
        spawnManager.maxFood    = selectedFood;

        // this kicks off your new InitializeSimulation()
        spawnManager.InitializeSimulation();

        // un-pause
        Time.timeScale = 1f;

        // swap to simulation view
        environmentContainer.SetActive(true);
        mainMenu       .SetActive(false);
        settingsMenu   .SetActive(false);
        aboutUsMenu    .SetActive(false);
        contactUsMenu  .SetActive(false);
    }

    // ─── Menu navigation ───────────────────────────────────────

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environmentContainer.SetActive(false);
    }

    public void OpenSettingsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environmentContainer.SetActive(false);
    }

    public void OpenAboutUsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(true);
        contactUsMenu.SetActive(false);
        environmentContainer.SetActive(false);
    }

    public void OpenContactUsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(true);
        environmentContainer.SetActive(false);
    }

    public void BackToMainMenu()
    {
        OpenMainMenu();
    }
}
