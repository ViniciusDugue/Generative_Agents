using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndSimMetricsUI : MonoBehaviour
{
    [Header("UI Texts")]
    public TMP_Text totalAgentsText;
    public TMP_Text agentsAliveText;
    public TMP_Text foodCollectedText;
    public TMP_Text foodLocationsDiscoveredText;
    public TMP_Text foodEatenByPestsText;
    public TMP_Text blocksMovedText;
    public TMP_Text wallsBuiltText;
    public TMP_Text wallsPlacedText;
    public TMP_Text simulationDurationText;

    [Header("Fitness Score Display")]
    public GameObject fitnessScorePrefab; // The Text prefab to instantiate
    public Transform fitnessScoreContainer; // The Vertical Layout Group to parent them under
    private List<GameObject> instantiatedFitnessTexts = new List<GameObject>(); // Track instantiated objects so we can clear them

    [Header("Tracked Values")]
    public int totalAgents = 0;
    public int deadAgents = 0;
    public int foodCollected = 0;
    public int foodLocationsDiscovered = 0;
    public int foodEatenByPests = 0;
    public int blocksMoved = 0;
    public int wallsBuilt = 0;
    public int wallsPlaced = 0;
    public int simulationDaysPassed = 0;

    public GameObject uiPanel;
    public static EndSimMetricsUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenUI()
    {
        
        uiPanel.SetActive(true);
        UpdateAllText();
        UpdateFitnessScores();
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
    }

    private void UpdateAllText()
    {
        SetSimulationDays(TimeManager.Instance.Days);
        SetTotalAgents(SpawnManager.Instance.maxAgents);

        totalAgentsText.text = $"Total Agents Count: {totalAgents}";
        agentsAliveText.text = $"Agents Alive: {totalAgents - deadAgents}";
        foodCollectedText.text = $"Food Collected: {foodCollected}";
        foodLocationsDiscoveredText.text = $"Food Locations Discovered: {foodLocationsDiscovered}";
        foodEatenByPestsText.text = $"Food Eaten by Pests: {foodEatenByPests}";
        blocksMovedText.text = $"Blocks Moved: {blocksMoved}";
        wallsBuiltText.text = $"Walls Built: {wallsBuilt}";
        wallsPlacedText.text = $"Walls Placed: {wallsPlaced}";
        simulationDurationText.text = $"Simulation Days Passed: {simulationDaysPassed}";
    }

    private void UpdateFitnessScores()
    {
        // Clear old fitness text objects
        foreach (var obj in instantiatedFitnessTexts)
        {
            Destroy(obj);
        }
        instantiatedFitnessTexts.Clear();

        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("SpawnManager.Instance not found.");
            return;
        }

        List<GameObject> aliveAgents = SpawnManager.Instance.aliveAgents;

        int agentNumber = 1;
        foreach (GameObject agentObj in aliveAgents)
        {
            if (agentObj != null)
            {
                BehaviorManager behaviorManager = agentObj.GetComponent<BehaviorManager>();
                if (behaviorManager != null)
                {
                    GameObject fitnessTextObj = Instantiate(fitnessScorePrefab, fitnessScoreContainer);
                    TMP_Text fitnessText = fitnessTextObj.transform.GetChild(0).GetComponent<TMP_Text>();
                    fitnessText.text = $"Agent {agentNumber}: {behaviorManager.FitnessScore:F2}";
                    instantiatedFitnessTexts.Add(fitnessTextObj);
                    agentNumber++;
                }
            }
        }
    }

    // Methods to increment counters
    public void IncrementFoodCollected() => foodCollected++;
    public void IncrementFoodEatenByPests() => foodEatenByPests++;
    public void IncrementBlocksMoved() => blocksMoved++;
    public void IncrementWallsBuilt() => wallsBuilt++;
    public void IncrementWallsPlaced() => wallsPlaced++;
    public void IncrementDeadAgents() => deadAgents++;
    public void IncrementFoodLocationsDiscovered() => foodLocationsDiscovered++;
    
    public void SetTotalAgents(int count) => totalAgents = count;
    public void SetSimulationDays(int days) => simulationDaysPassed = days;

    //EndSimMetricsUI.Instance.SetTotalAgents(maxAgents);
    //SetSimulationDays(TimeManager.Instance.Days);
    //EndSimMetricsUI.Instance.IncrementFoodCollected();
    //EndSimMetricsUI.Instance.IncrementFoodEatenByPests();
    //EndSimMetricsUI.Instance.IncrementBlocksMoved();
    //EndSimMetricsUI.Instance.IncrementWallsBuilt();
    //EndSimMetricsUI.Instance.IncrementWallsPlaced();
    //EndSimMetricsUI.Instance.IncrementDeadAgents();
}

