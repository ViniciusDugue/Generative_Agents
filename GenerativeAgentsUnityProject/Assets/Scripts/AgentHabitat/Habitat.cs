using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Habitat : MonoBehaviour
{
    [Header("Habitat Settings")]
    [Tooltip("The central hub point for agents to gather. If left unassigned, the habitat will reposition itself at a random agent spawn point.")]
    public Transform centralHubPoint;
    // [Tooltip("Total food stock available at the habitat.")]
    // public int foodPortionsAvailable = 100;
    [Tooltip("Time interval (in seconds) at which food is dispensed.")]
    public float dispenseInterval = 5f;
    [Tooltip("The food portion value dispensed to each agent.")]
    public int foodPortionValue = 10;

    [Header("In-Sim Variables")]
    [SerializeField]
    public int storedFood = 0;

    [Header("Dispensation Settings")]
    [Tooltip("Expected number of agents (or threshold of agents) that should be registered.")]
    public int expectedAgentCount = 2;


    // List of agents waiting for food at the habitat.
    private List<AgentHeal> waitingAgents = new List<AgentHeal>();

    private void Awake()
    {
        // If no central hub is assigned, reposition the habitat.
        if (centralHubPoint == null)
        {
            RepositionHabitat();
        }
    }

    private void Start()
    {
        // Start dispensing food periodically.
        StartCoroutine(CheckForDispenseCondition());
    }

    // Call this method to reposition the habitat to a random agent spawn point.
    public void RepositionHabitat()
    {
        GameObject[] agentSpawnPoints = GameObject.FindGameObjectsWithTag("agentSpawn");
        if (agentSpawnPoints.Length > 0)
        {
            int index = Random.Range(0, agentSpawnPoints.Length);
            Transform chosenSpawn = agentSpawnPoints[index].transform;
            centralHubPoint = chosenSpawn;  // Optionally update the central hub reference.
            transform.position = chosenSpawn.position;
            transform.rotation = chosenSpawn.rotation;
            Debug.Log("Habitat repositioned to agent spawn point: " + chosenSpawn.position);
        }
        else
        {
            Debug.LogWarning("No agent spawn points found; habitat cannot reposition.");
        }
    }

    // Called when an agent arrives at the habitat.
    public void RegisterAgent(AgentHeal agent)
    {
        if (!waitingAgents.Contains(agent))
        {
            waitingAgents.Add(agent);
            Debug.Log("Agent registered at habitat: " + agent.name);
        }
        else 
        {
            Debug.LogWarning("AgentHeal component missing" + GetComponent<Collider>());
        }
    }

    // Called when an agent leaves the habitat.
    public void UnregisterAgent(AgentHeal agent)
    {
        if (waitingAgents.Contains(agent))
        {
            waitingAgents.Remove(agent);
            Debug.Log("Agent unregistered from habitat: " + agent.name);
        }
    }

    public void HandleAgentEntry(Collider collider)
    {
        Debug.Log("Agent entered Habitat door trigger: " + collider.name);
        
        // Register the agent using its AgentHeal component.
        AgentHeal agentHeal = collider.GetComponent<AgentHeal>();
        if (agentHeal != null)
        {
            RegisterAgent(agentHeal);
        }
        else
        {
            Debug.LogWarning("AgentHeal component missing on " + collider.name);
        }
        
        // Trigger food drop after a delay.
        BehaviorManager bm = collider.GetComponent<BehaviorManager>();
        if (bm != null)
        {
            StartCoroutine(DropFoodAfterDelay(collider, bm));
        }
        else
        {
            Debug.LogWarning("No BehaviorManager found on " + collider.name);
        }
    }


    IEnumerator DropFoodAfterDelay(Collider collider, BehaviorManager bm)
    {
        yield return new WaitForSeconds(1f);
        if (collider.CompareTag("agent") && bm.getFood() > 0)
        {
            // Instead of dropping food one unit at a time,
            // deposit all food the agent is carrying.
            int deposited = bm.DepositAllFood();
            storedFood += deposited;
            Debug.Log($"Agent {collider.name} deposited {deposited} food. Habitat stored food now = {storedFood}");
        }
    }

    // Coroutine that checks if the expected number of agents are registered and if they have dropped food.
    private IEnumerator CheckForDispenseCondition()
    {
        while (true)
        {
            // Check every 1 second.
            yield return new WaitForSeconds(1f);

            // If the number of registered agents is at least the expected count…
            if (waitingAgents.Count >= expectedAgentCount)
            {
                bool allDropped = true;
                // Check if every registered agent has dropped its food.
                foreach (var agent in waitingAgents)
                {
                    BehaviorManager bm = agent.GetComponent<BehaviorManager>();
                    if (bm != null && bm.getFood() > 0)
                    {
                        allDropped = false;
                        break;
                    }
                }
                if (allDropped)
                {
                    Debug.Log("All registered agents have dropped their food. Dispensing food based on fitness.");
                    DispenseFood();
                    // Optionally clear the list or wait for a new registration cycle.
                    waitingAgents.Clear();
                    break;
                }
                else
                {
                    Debug.Log("Waiting for all agents to drop their food...");
                }
            }
            else
            {
                Debug.Log($"Waiting agents: {waitingAgents.Count} of expected {expectedAgentCount}.");
            }
        }
    }

    // Dispenses food in order of fitness (highest first).
    public void DispenseFood()
    {
        // Sort waiting agents by descending fitness score,
        // and if the scores are equal, by ascending agentID.
        var sortedAgents = waitingAgents
            .OrderByDescending(a => a.GetComponent<BehaviorManager>().calculateFitnessScore())
            .ThenBy(a => a.GetComponent<BehaviorManager>().agentID)
            .ToList();

        Debug.Log("Dispensing food in the following order:");
        foreach (var agent in sortedAgents)
        {
            var bm = agent.GetComponent<BehaviorManager>();
            float fitness = bm.calculateFitnessScore();
            Debug.Log($"{agent.name}: Fitness = {fitness}, AgentID = {bm.agentID}");

            // Dispense food portion to the agent.
            agent.ReceiveFood(foodPortionValue);

            // Decrement storedFood by the portion that was dispensed.
            storedFood -= foodPortionValue;
            if (storedFood < 0)
                storedFood = 0;

            Debug.Log($"Stored food remaining: {storedFood}");
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

    IEnumerator dropFood(Collider collider, BehaviorManager bm) {
        yield return new WaitForSeconds(1f);
        if (collider.CompareTag("agent") && bm.getFood() > 0) {
            bm.dropFood();
            storedFood += 1;
            if (bm.getFood() <= 0) {
                Debug.Log("Agent has deposisted all food");
            }
        }

    }

}
