using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Habitat : MonoBehaviour
{
    [Header("Habitat Settings")]
    [Tooltip("The central hub point for agents to gather. If left unassigned, the habitat will reposition itself at a random agent spawn point.")]
    public Transform centralHubPoint;
    [Tooltip("Total food stock available at the habitat.")]
    public int foodPortionsAvailable = 100;
    [Tooltip("Time interval (in seconds) at which food is dispensed.")]
    public float dispenseInterval = 5f;
    [Tooltip("The food portion value dispensed to each agent.")]
    public int foodPortionValue = 10;

    [Header("In-Sim Variables")]
    [SerializeField]
    public int storedFood = 0;


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
        StartCoroutine(DispenseFoodRoutine());
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

    private void OnTriggerEnter(Collider collider)
    {
        // Debug.Log($"OnTriggerEnter called: {collider.name}, tag: {collider.tag}");
        if (collider.CompareTag("agent"))
        {
            Debug.Log("Entered Habitat Collider");

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

            // Also get the BehaviorManager to call dropFood.
            BehaviorManager bm = collider.GetComponent<BehaviorManager>();
            if (bm != null)
            {
                StartCoroutine(dropFood(collider, bm));
            }
            else
            {
                Debug.LogWarning("No BehaviorManager found on " + collider.name);
            }
        }
        else
        {
            Debug.LogWarning("Collider does not have tag 'agent'. It is: " + collider.tag);
        }
    }


    // Periodically dispenses food.
    private IEnumerator DispenseFoodRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(dispenseInterval);
            DispenseFood();
        }
    }

    // Dispenses food in order of fitness (highest first).
    public void DispenseFood()
    {
        // Debug.Log("Waiting agents count: " + waitingAgents.Count);
        if (waitingAgents.Count == 0)
        {
            // Debug.Log("No agents registered in waitingAgents!");
            return;
        }
        // Order the agents by descending fitness score, then by ascending agentID.
        var sortedAgents = waitingAgents.OrderByDescending(a => a.GetComponent<BehaviorManager>().calculateFitnessScore())
                                        .ThenBy(a => a.GetComponent<BehaviorManager>().agentID)
                                        .ToList();

        Debug.Log("Dispensing food in the following order:");
        foreach (var agent in sortedAgents)
        {
            var bm = agent.GetComponent<BehaviorManager>();
            float fitness = bm.calculateFitnessScore();
            Debug.Log($"{agent.name}: Fitness = {fitness}, AgentID = {bm.agentID}");
            
            agent.ReceiveFood(foodPortionValue);
            foodPortionsAvailable -= foodPortionValue;
            storedFood += foodPortionValue;
            if (foodPortionsAvailable <= 0)
            {
                Debug.Log("Habitat out of food!");
                break;
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
