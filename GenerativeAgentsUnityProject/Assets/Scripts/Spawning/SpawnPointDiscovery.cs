using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointDiscovery : MonoBehaviour
{
    [Tooltip("The detection range within which a spawn point is discovered.")]
    public float detectionRange = 5f;

    private SpawnManager spawnManager;
    private AgentMapInfo agentMapInfo;

    // Keep track of already discovered spawn points.
    private HashSet<Transform> discoveredSpawnPoints = new HashSet<Transform>();

    void Start()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager not found in the scene.");
        }

        // Attempt to get the agentMapInfo component on the agent.
        agentMapInfo = GetComponent<AgentMapInfo>();
        if (agentMapInfo == null)
        {
            Debug.LogWarning("AgentMapInfo component not found on agent; discoveries won't be recorded in agent memory.");
        }
    }

    void Update()
    {
        if (spawnManager == null)
            return;

        // Iterate through all active food spawn points.
        foreach (Transform spawnPoint in spawnManager.ActiveFoodSpawnPoints)
        {
            float distance = Vector3.Distance(transform.position, spawnPoint.position);
            // If within range and not already discovered:
            if (distance <= detectionRange && !discoveredSpawnPoints.Contains(spawnPoint))
            {
                discoveredSpawnPoints.Add(spawnPoint);

                // Optionally record the discovery in the agent's AgentMapInfo.
                if (agentMapInfo != null)
                {
                    AgentMapInfo.MarkerData spawnMarkerData = new AgentMapInfo.MarkerData()
                    {
                        discoveredObject = spawnPoint.gameObject,
                        markerType = MarkerEventManager.MarkerType.FoodSpawn
                    };
                    agentMapInfo.knownMarkers.Add(spawnMarkerData);
                }

                // Raise an event for the discovered food spawn point.
                MarkerEventManager.MarkerSpawned(spawnPoint.gameObject, MarkerEventManager.MarkerType.FoodSpawn);
                Debug.Log($"{gameObject.name} discovered spawn point: {spawnPoint.name}");

                // Check if there is food spawned from this spawn point.
                if (spawnManager.FoodSpawnMapping.TryGetValue(spawnPoint, out List<GameObject> foodList))
                {
                    foreach (GameObject food in foodList)
                    {
                        if (food != null)
                        {
                            // Optionally record the food discovery in the agent's AgentMapInfo.
                            if (agentMapInfo != null)
                            {
                                AgentMapInfo.MarkerData foodMarkerData = new AgentMapInfo.MarkerData()
                                {
                                    discoveredObject = food,
                                    markerType = MarkerEventManager.MarkerType.Food
                                };
                                agentMapInfo.knownMarkers.Add(foodMarkerData);
                            }
                            // Raise an event for the discovered food.
                            MarkerEventManager.MarkerSpawned(food, MarkerEventManager.MarkerType.Food);
                            Debug.Log($"{gameObject.name} discovered food: {food.name} from spawn point: {spawnPoint.name}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"{gameObject.name} did not find any food mapped for spawn point: {spawnPoint.name}");
                }
            }
        }
    }
}
