using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AgentUIManager : MonoBehaviour
{
    public static AgentUIManager Instance { get; private set; }

    [Header("Agent Health UI")]
    public GameObject healthBarObject;   // GameObject holding the slider
    public Slider healthSlider;
    public Image fillImage;
    public Gradient healthGradient;

    [Header("Agent Stats UI")]
    public GameObject agentPanel;        // Root of the toggleable left panel
    public GameObject foodContainer;     // Parent with HorizontalLayoutGroup
    public GameObject foodIconPrefab;    // Food sprite prefab to show per unit of food

    public TextMeshProUGUI agentIDText;
    public TextMeshProUGUI exhaustionText;
    public TextMeshProUGUI foodHeldText;
    public TextMeshProUGUI foodDepositedText;
    public TextMeshProUGUI fitnessText;
    public TextMeshProUGUI behaviorText;

    private AgentHealth currentAgent;

    private BehaviorManager currentBehavior;
    private int lastKnownFood = -1; // To track food change

    private AgentHeal currentHeal;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        agentPanel.SetActive(false);
    }
    public void ShowAgentStats(AgentHealth agentHealth, BehaviorManager behavior)
    {
        currentAgent = agentHealth;
        currentBehavior = behavior;
        lastKnownFood = -1; // force refresh on first show

        agentPanel.SetActive(true);
        healthBarObject.SetActive(true);

        // Health
        healthSlider.maxValue = agentHealth.maxHealth;
        healthSlider.value = agentHealth.currentHealth;
        fillImage.color = healthGradient.Evaluate(healthSlider.normalizedValue);
        currentHeal = agentHealth.GetComponent<AgentHeal>();

        UpdateFoodIcons(); // Draw food icons immediately
        UpdateAgentStats();
    }

    public void HideAgentStats()
    {
        currentAgent = null;
        agentPanel.SetActive(false);
    }

    private void UpdateAgentStats()
    {
        if (currentBehavior == null) return;

        agentIDText.text = $"ID: {currentBehavior.agentID}";
        exhaustionText.text = $"Exhaustion: {currentBehavior.exhaustion:F1}";
        foodHeldText.text = $"Carrying: {currentBehavior.getFood()}";
        foodDepositedText.text = $"Deposited: {currentBehavior.depositedFood}";
        fitnessText.text = $"Fitness: {currentBehavior.FitnessScore:F1}";

        if (currentBehavior.currentAgentBehavior != null)
            behaviorText.text = $"Behavior: {currentBehavior.currentAgentBehavior.GetType().Name}";
        else
            behaviorText.text = "Behavior: Unknown";
    }

    private void UpdateFoodIcons()
    {
        if (currentHeal == null || foodContainer == null)
            return;

        int consumedFood = currentHeal.foodPortionsReceived; // from AgentHeal
        int maxPortions = 5;

        // Skip if there's no change
        if (consumedFood == lastKnownFood) return;

        lastKnownFood = consumedFood;

        // Clear old icons
        foreach (Transform child in foodContainer.transform)
            Destroy(child.gameObject);

        // Instantiate icons based on consumption
        for (int i = 0; i < maxPortions; i++)
        {
            GameObject icon = Instantiate(foodIconPrefab, foodContainer.transform);
            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = (i < consumedFood) ? 1f : 0.2f;
                img.color = c;
            }
        }
    }


    void Update()
    {
        // Detect left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore clicks on UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            // Raycast ignoring spawn layers
            int layerMask = ~LayerMask.GetMask("FoodSpawn", "EnemySpawn", "AgentSpawn");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                HideAgentStats();
            }
            else
            {
                // Safe access even if we hit a child visual or food object
                AgentHealth clickedHealth = hit.collider.GetComponentInParent<AgentHealth>();
                BehaviorManager behavior = hit.collider.GetComponentInParent<BehaviorManager>();

                if (clickedHealth == null || behavior == null)
                {
                    HideAgentStats();
                }
                else
                {
                    ShowAgentStats(clickedHealth, behavior);
                }
            }
        }

        // Live updates if active
        if (currentAgent != null && agentPanel.activeSelf)
        {
            healthSlider.value = currentAgent.currentHealth;
            fillImage.color = healthGradient.Evaluate(healthSlider.normalizedValue);

            UpdateFoodIcons();
            UpdateAgentStats();
        }
    }
}