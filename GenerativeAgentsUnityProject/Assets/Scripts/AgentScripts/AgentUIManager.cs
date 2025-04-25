using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Needed to detect UI clicks

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

    private AgentHealth currentAgent;

    private BehaviorManager currentBehavior;
    private int lastKnownFood = -1; // To track food change


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

        UpdateFoodIcons(); // Draw food icons immediately
    }

    public void HideAgentStats()
    {
        currentAgent = null;
        agentPanel.SetActive(false);
    }

    private void UpdateFoodIcons()
    {
        if (currentBehavior == null || foodContainer == null) return;

        int currentFood = currentBehavior.getFood();
        if (currentFood == lastKnownFood) return; // Skip if nothing changed

        lastKnownFood = currentFood;

        // Clear existing
        foreach (Transform child in foodContainer.transform)
            Destroy(child.gameObject);

        int maxFood = 5;
        for (int i = 0; i < maxFood; i++)
        {
            GameObject icon = Instantiate(foodIconPrefab, foodContainer.transform);

            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = (i < currentFood) ? 1f : 0.2f;
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
                // ✅ Safe access even if we hit a child visual or food object
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

        // 🔁 Live health bar updates if active
        if (currentAgent != null && agentPanel.activeSelf)
        {
            healthSlider.value = currentAgent.currentHealth;
            fillImage.color = healthGradient.Evaluate(healthSlider.normalizedValue);

            // Food live update
            UpdateFoodIcons();
        }
    }
}