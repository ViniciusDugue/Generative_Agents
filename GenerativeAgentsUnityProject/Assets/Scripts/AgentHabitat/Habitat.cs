using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Habitat : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Radius around the habitat to detect agents for food drop.")]
    public float triggerRadius = 5f;

    [Header("Habitat Settings")]
    [Tooltip("The central hub point for agents to gather. If left unassigned, the habitat will reposition itself at a random agent spawn point.")]
    public Transform centralHubPoint;
    [Tooltip("Time interval (in seconds) at which food is dispensed.")]
    public float dispenseInterval = 5f;
    [Tooltip("The food portion value dispensed to each agent.")]
    public int foodPortionValue = 1;
    [Tooltip("Expected number of agents (or threshold of agents) that should be registered before dispensing.")]
    public int expectedAgentCount = 2;

    [Header("In‑Sim Variables")]
    [SerializeField]
    public int storedFood = 0;
    public int storedBlocks = 0;
    public int wallsBuilt = 0;

    public List<GameObject> wallsList = new List<GameObject>();
    private int nextWallIndex = 0;

    private List<AgentHeal> waitingAgents = new List<AgentHeal>();
    private SphereCollider triggerCollider;

    public static Habitat Instance;
    public bool isGuarded;

    void Awake()
    {
        Instance = this;

        // Ensure we have a trigger collider at the right radius
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;

        // If no hub assigned, reposition as before
        if (centralHubPoint == null)
            RepositionHabitat();
    }

    void Start()
    {
        // Begin the periodic dispense check
        StartCoroutine(CheckForDispenseCondition());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            var agentHeal = other.GetComponent<AgentHeal>();
            if (agentHeal != null)
            {
                RegisterAgent(agentHeal);
                var bm = other.GetComponent<BehaviorManager>();
                if (bm != null)
                    StartCoroutine(DropFoodAfterDelay(other, bm));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            var agentHeal = other.GetComponent<AgentHeal>();
            if (agentHeal != null)
                UnregisterAgent(agentHeal);
        }
    }

    /// <summary>
    /// If no central hub is assigned, reposition the habitat to a random agent spawn point.
    /// </summary>
    public void RepositionHabitat()
    {
        GameObject[] agentSpawnPoints = GameObject.FindGameObjectsWithTag("agentSpawn");
        if (agentSpawnPoints.Length > 0)
        {
            int index = Random.Range(0, agentSpawnPoints.Length);
            Transform chosenSpawn = agentSpawnPoints[index].transform;
            centralHubPoint = chosenSpawn;
            transform.position = chosenSpawn.position;
            transform.rotation = chosenSpawn.rotation;
            Debug.Log("Habitat repositioned to agent spawn point: " + chosenSpawn.position);
        }
        else
        {
            Debug.LogWarning("No agent spawn points found; habitat cannot reposition.");
        }
    }

    /// <summary>
    /// Called when an agent enters the radius.
    /// </summary>
    public void RegisterAgent(AgentHeal agent)
    {
        if (!waitingAgents.Contains(agent))
        {
            waitingAgents.Add(agent);
            Debug.Log("Agent registered at habitat: " + agent.name);

            // Check if the Agent is guarding the habitat
            GuardBehavior guardBehavior = agent.GetComponent<GuardBehavior>();
            if (guardBehavior.enabled == true) {
                isGuarded = true;
            }
        }
    }

    /// <summary>
    /// Called when an agent leaves the radius.
    /// </summary>
    public void UnregisterAgent(AgentHeal agent)
    {
        if (waitingAgents.Contains(agent))
        {
            waitingAgents.Remove(agent);
            Debug.Log("Agent unregistered from habitat: " + agent.name);
        }
    }

    /// <summary>
    /// After a short delay, deposit all the agent’s food and add it to storedFood.
    /// </summary>
    IEnumerator DropFoodAfterDelay(Collider collider, BehaviorManager bm)
    {
        yield return new WaitForSeconds(1f);
        if (collider.CompareTag("agent") && bm.getFood() > 0)
        {
            int deposited = bm.DepositAllFood();
            storedFood += deposited;
            Debug.Log($"Agent {collider.name} deposited {deposited} food. Habitat stored food now = {storedFood}");
        }
    }

    /// <summary>
    /// Every second, once enough agents are in range and have dropped their food,
    /// dispense based on fitness and clear the waiting list.
    /// </summary>
    private IEnumerator CheckForDispenseCondition()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (waitingAgents.Count >= expectedAgentCount)
            {
                bool allDropped = true;
                foreach (var agent in waitingAgents)
                {
                    var bm = agent.GetComponent<BehaviorManager>();
                    if (bm != null && bm.getFood() > 0)
                    {
                        allDropped = false;
                        break;
                    }
                }

                if (allDropped && storedFood > 0)
                {
                    Debug.Log("All registered agents have dropped their food. Dispensing food based on fitness.");
                    DispenseFood();
                    waitingAgents.Clear();
                    break;
                }
                else if (!allDropped)
                {
                    Debug.Log("Waiting for all agents to drop their food...");
                }
                else if (storedFood <= 0) {
                    Debug.Log("No food available to dispense.");
                }
            }
            else
            {
                // Debug.Log($"Waiting agents: {waitingAgents.Count} of expected {expectedAgentCount}.");
            }
        }
    }

    /// <summary>
    /// Dispense food in order of fitness (highest first), decrementing storedFood.
    /// </summary>
    public void DispenseFood()
    {
        var sortedAgents = waitingAgents
            .OrderByDescending(a => a.GetComponent<BehaviorManager>().calculateFitnessScore())
            .ThenBy(a => a.GetComponent<BehaviorManager>().agentID)
            .ToList();

        Debug.Log("Dispensing food in the following order:");
        foreach (var agent in sortedAgents)
        {
            if (storedFood > 0) {
                // Get Agent Food & Fitness Metrics
                var bm = agent.GetComponent<BehaviorManager>();
                var ah = agent.GetComponent<AgentHeal>();
                float fitness = bm.FitnessScore;
                int remainingHunger = bm.RequiredFood - bm.CurrentHunger;

                Debug.Log($"{agent.name}: Fitness = {fitness}, AgentID = {bm.agentID}");
                // Calculate if there's enough food to dispense to fully satisfy this agent
                if (remainingHunger > storedFood) {
                    Debug.Log("Not Enough Food to fully satisfy Agent Hunger");
                    agent.ReceiveFood(storedFood);
                    RemoveFood(storedFood);
                    break;
                }

                // Dispense food to the agent
                agent.ReceiveFood(remainingHunger);
                RemoveFood(remainingHunger);
                Debug.Log($"Stored food remaining: {storedFood}");
            }
            
        }
    }

    // Draw a visual representation of the central hub and spawn points.
    private void OnDrawGizmos()
    {
        // Draw the central hub (cyan).
        if (centralHubPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(centralHubPoint.position, 1f);
        }
        
        // Draw agent spawn points in blue.
        GameObject[] agentSpawns = GameObject.FindGameObjectsWithTag("agentSpawn");
        foreach (GameObject spawn in agentSpawns)
        {
            SphereCollider sc = spawn.GetComponent<SphereCollider>();
            float radius = (sc != null) ? sc.radius : 1f;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawn.transform.position, radius);
        }

        // Draw enemy spawn points in red.
        GameObject[] enemySpawns = GameObject.FindGameObjectsWithTag("enemySpawn");
        foreach (GameObject spawn in enemySpawns)
        {
            SphereCollider sc = spawn.GetComponent<SphereCollider>();
            float radius = (sc != null) ? sc.radius : 1f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawn.transform.position, radius);
        }

        // Draw food spawn points in green.
        GameObject[] foodSpawns = GameObject.FindGameObjectsWithTag("foodSpawn");
        foreach (GameObject spawn in foodSpawns)
        {
            SphereCollider sc = spawn.GetComponent<SphereCollider>();
            float radius = (sc != null) ? sc.radius : 1f;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawn.transform.position, radius);
        }
    }

    public void RemoveFood(int foodRemoved)
    {   
        if(storedFood - foodRemoved>0)
        {
            storedFood -=foodRemoved;
        }
        else{
            storedFood = 0;
        }
        
    }

    public void BuildNextWall()
    {
        if (nextWallIndex < wallsList.Count || storedBlocks>=3)
        {
            var wall = wallsList[nextWallIndex];
            wall.SetActive(true);
            Debug.Log($"[Habitat] Built wall #{nextWallIndex + 1}: {wall.name}");
            nextWallIndex++;
            wallsBuilt++;
            storedBlocks-=3;
            EndSimMetricsUI.Instance.IncrementWallsBuilt();
        }
        else
        {
            Debug.LogWarning("[Habitat] All walls have already been built! or Not Enough Stored Blocks.");
        }
    }

    public GameObject GetNextWallToBuild()
    {
        if (nextWallIndex < wallsList.Count)
            return wallsList[nextWallIndex];
        return null;
    }
}
