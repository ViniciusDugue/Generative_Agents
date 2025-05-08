using System.Collections.Generic;
using UnityEngine;
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
    public GameObject fitnessScorePrefab;    // prefab with a TMP_Text child
    public Transform fitnessScoreContainer;  // vertical layout group parent
    private List<GameObject> instantiatedFitnessTexts = new List<GameObject>();

    [Header("Tracked Values (you can increment these as your sim runs)")]
    public int totalAgents = 0;
    public int deadAgents = 0;
    public int foodCollected = 0;
    public int foodLocationsDiscovered = 0;
    public int foodEatenByPests = 0;
    public int blocksMoved = 0;
    public int wallsBuilt = 0;
    public int wallsPlaced = 0;
    public int simulationDaysPassed = 0;

    [Header("Panel")]
    public GameObject uiPanel;
    public static EndSimMetricsUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Call this when you want to pop open the metrics screen.
    /// </summary>
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
        var mgr = SpawnManager.Instance;
        var tm  = TimeManager.Instance;

        if (mgr != null)
        {
            totalAgents       = mgr.spawnedAgents.Count;
            int alive         = mgr.aliveAgents.Count;
            int food          = mgr.spawnedFood.Count;
            int spots         = mgr.ActiveFoodSpawnPoints.Count;
            int pests         = mgr.SpawnedPestsCount;

            totalAgentsText.text             = $"Total Agents: {totalAgents}";
            agentsAliveText.text             = $"Agents Alive: {alive}";
            foodCollectedText.text           = $"Food Collected: {food}";
            foodLocationsDiscoveredText.text = $"Food Spawns Active: {spots}";
            foodEatenByPestsText.text        = $"Food Eaten by Pests: {pests}";
        }
        else
        {
            Debug.LogWarning("SpawnManager.Instance is null!");
        }

        // use your own tracked counters here:
        blocksMovedText.text = $"Blocks Moved: {blocksMoved}";
        wallsBuiltText.text  = $"Walls Built: {wallsBuilt}";
        wallsPlacedText.text = $"Walls Placed: {wallsPlaced}";

        int days = tm != null ? tm.Days : 0;
        simulationDurationText.text = $"Days Passed: {days}";
    }

    private void UpdateFitnessScores()
    {
        // clear out old entries
        foreach (var go in instantiatedFitnessTexts)
            Destroy(go);
        instantiatedFitnessTexts.Clear();

        if (SpawnManager.Instance == null) return;

        int agentNumber = 1;
        foreach (var agentObj in SpawnManager.Instance.aliveAgents)
        {
            if (agentObj == null) continue;
            var bm = agentObj.GetComponent<BehaviorManager>();
            if (bm == null) continue;

            var entry = Instantiate(fitnessScorePrefab, fitnessScoreContainer);
            var txt = entry.GetComponentInChildren<TMP_Text>();
            txt.text = $"Agent {agentNumber++}: {bm.FitnessScore:F2}";
            instantiatedFitnessTexts.Add(entry);
        }
    }

    // if you want to update these mid-sim, call:
    public void IncrementFoodCollected()        => foodCollected++;
    public void IncrementFoodEatenByPests()     => foodEatenByPests++;
    public void IncrementBlocksMoved()           => blocksMoved++;
    public void IncrementWallsBuilt()            => wallsBuilt++;
    public void IncrementWallsPlaced()           => wallsPlaced++;
    public void IncrementDeadAgents()            => deadAgents++;
    public void IncrementFoodLocationsDiscovered() => foodLocationsDiscovered++;
    public void SetTotalAgents(int c)            => totalAgents = c;
    public void SetSimulationDays(int d)         => simulationDaysPassed = d;
}
