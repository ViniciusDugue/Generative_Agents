using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointDiscovery : MonoBehaviour
{
    [Tooltip("The detection range within which a spawn point is discovered.")]
    public float detectionRange = 5f;

    private SpawnManager      spawnManager;
    private AgentMapInfo      agentMapInfo;
    private HashSet<Transform> discoveredSpawnPoints = new();

    void Start()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager == null)
            Debug.LogError("SpawnManager not found in the scene.");

        agentMapInfo = GetComponent<AgentMapInfo>();
        if (agentMapInfo == null)
            Debug.LogWarning("AgentMapInfo component not found on agent; discoveries won't be recorded.");
    }

    void Update()
    {
        if (spawnManager == null) return;

        foreach (Transform spawnPoint in spawnManager.ActiveFoodSpawnPoints)
        {
            float dist = Vector3.Distance(transform.position, spawnPoint.position);
            if (dist <= detectionRange && !discoveredSpawnPoints.Contains(spawnPoint))
            {
                discoveredSpawnPoints.Add(spawnPoint);

                // 1) Record the spawn‑point discovery in this agent's memory
                if (agentMapInfo != null)
                {
                    var data = new AgentMapInfo.MarkerData(
                        spawnPoint.gameObject,
                        MarkerEventManager.MarkerType.FoodSpawn
                    );
                    agentMapInfo.knownMarkers.Add(data);
                }

                // 2) Fire the global event so MapMarkerManager will render it
                MarkerEventManager.MarkerSpawned(
                    spawnPoint.gameObject,
                    MarkerEventManager.MarkerType.FoodSpawn
                );
                Debug.Log($"{gameObject.name} discovered spawn point: {spawnPoint.name}");

                // 3) Now look up any actual food items at that spawn point
                if (spawnManager.FoodSpawnMapping.TryGetValue(spawnPoint, out var foodList))
                {
                    foreach (var food in foodList)
                    {
                        if (food == null) continue;

                        if (agentMapInfo != null)
                        {
                            var foodData = new AgentMapInfo.MarkerData(
                                food,
                                MarkerEventManager.MarkerType.Food
                            );
                            agentMapInfo.knownMarkers.Add(foodData);
                        }

                        MarkerEventManager.MarkerSpawned(
                            food,
                            MarkerEventManager.MarkerType.Food
                        );
                        Debug.Log($"{gameObject.name} discovered food: {food.name}");
                    }
                }
                else
                {
                    Debug.Log($"{gameObject.name} found no food at {spawnPoint.name}");
                }
            }
        }
    }
}