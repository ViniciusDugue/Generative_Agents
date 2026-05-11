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

        // ── Discover *all* food spawn points ──
        foreach (Transform spawnPoint in spawnManager.FoodSpawnPoints)
        {
            float distance = Vector3.Distance(transform.position, spawnPoint.position);
            if (distance <= detectionRange && !discoveredSpawnPoints.Contains(spawnPoint))
            {
                discoveredSpawnPoints.Add(spawnPoint);

                // Record the spawn‐point discovery
                if (agentMapInfo != null)
                {
                    var spawnData = new AgentMapInfo.MarkerData()
                    {
                        discoveredObject = spawnPoint.gameObject,
                        markerType       = MarkerEventManager.MarkerType.FoodSpawn
                    };
                    agentMapInfo.knownMarkers.Add(spawnData);
                }

                MarkerEventManager.MarkerSpawned(spawnPoint.gameObject, MarkerEventManager.MarkerType.FoodSpawn);
                Debug.Log($"{gameObject.name} discovered food spawn point: {spawnPoint.name}");

                // If any food is mapped here, discover it too
                if (spawnManager.FoodSpawnMapping.TryGetValue(spawnPoint, out List<GameObject> foodList))
                {
                    foreach (GameObject food in foodList)
                    {
                        if (food == null) continue;

                        if (agentMapInfo != null)
                        {
                            var foodData = new AgentMapInfo.MarkerData()
                            {
                                discoveredObject = food,
                                markerType       = MarkerEventManager.MarkerType.Food
                            };
                            agentMapInfo.knownMarkers.Add(foodData);
                        }
                        MarkerEventManager.MarkerSpawned(food, MarkerEventManager.MarkerType.Food);
                        Debug.Log($"{gameObject.name} discovered food: {food.name} at {spawnPoint.name}");
                    }
                }
            }
        }

        // ── Discover *all* enemy spawn points ──
        foreach (Transform spawnPoint in spawnManager.EnemySpawnPoints)
        {
            float distance = Vector3.Distance(transform.position, spawnPoint.position);
            if (distance <= detectionRange && !discoveredSpawnPoints.Contains(spawnPoint))
            {
                discoveredSpawnPoints.Add(spawnPoint);

                if (agentMapInfo != null)
                {
                    var enemySpawnData = new AgentMapInfo.MarkerData()
                    {
                        discoveredObject = spawnPoint.gameObject,
                        markerType       = MarkerEventManager.MarkerType.EnemySpawn
                    };
                    agentMapInfo.knownMarkers.Add(enemySpawnData);
                }

                MarkerEventManager.MarkerSpawned(spawnPoint.gameObject, MarkerEventManager.MarkerType.EnemySpawn);
                Debug.Log($"{gameObject.name} discovered enemy spawn point: {spawnPoint.name}");
            }
        }
    }

}
