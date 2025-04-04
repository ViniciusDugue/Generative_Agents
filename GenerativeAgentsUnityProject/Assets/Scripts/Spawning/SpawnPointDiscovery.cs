using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointDiscovery : MonoBehaviour
{
    [Tooltip("The detection range within which a spawn point is discovered.")]
    public float detectionRange = 5f;

    private SpawnManager spawnManager;

    // Declare discoveredSpawnPoints to keep track of already discovered spawn points.
    private HashSet<Transform> discoveredSpawnPoints = new HashSet<Transform>();

    void Start()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager not found in the scene.");
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
                            // Raise an event for each food object.
                            MarkerEventManager.MarkerSpawned(food, MarkerEventManager.MarkerType.Food);
                            Debug.Log($"{gameObject.name} discovered food: {food.name} from spawn point: {spawnPoint.name}");
                        }
                    }
                }
            }
        }
    }
}
