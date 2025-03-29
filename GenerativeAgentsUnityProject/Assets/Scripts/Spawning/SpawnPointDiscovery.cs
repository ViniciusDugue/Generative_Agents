using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointDiscovery : MonoBehaviour
{
    [Tooltip("The detection range within which a spawn point is discovered.")]
    public float detectionRange = 5f;

    private SpawnManager spawnManager;

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
        if (spawnManager != null)
        {
            // Debug.Log("ActiveFoodSpawnPoints count: " + spawnManager.ActiveFoodSpawnPoints.Count);
            // Loop through all active food spawn points.
            foreach (Transform spawnPoint in spawnManager.ActiveFoodSpawnPoints)
            {
                float distance = Vector3.Distance(transform.position, spawnPoint.position);
                // Debug.Log($"Checking spawn point '{spawnPoint.name}' at distance {distance:F2}");
                if (distance <= detectionRange)
                {
                    // Debug.Log($"Agent '{gameObject.name}' is within detection range ({distance:F2}) of spawn point '{spawnPoint.name}'");

                    // Register the spawn point marker on all map managers.
                    MapMarkerManager[] markerManagers = FindObjectsOfType<MapMarkerManager>();
                    foreach (MapMarkerManager manager in markerManagers)
                    {
                        // Debug.Log("Registering spawn marker on map container: " + manager.mapContainer.name);
                        manager.RegisterDiscoveredSpawnPoint(spawnPoint.gameObject, "foodSpawn");
                    }

                    // Now check the mapping in SpawnManager to get food spawned from this spawn point.
                    if (spawnManager.FoodSpawnMapping.TryGetValue(spawnPoint, out List<GameObject> foodList))
                    {
                        foreach (GameObject food in foodList)
                        {
                            if (food != null)
                            {
                                foreach (MapMarkerManager manager in markerManagers)
                                {
                                    // Register a food marker on each map.
                                    manager.RegisterDiscoveredFood(food);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Debug.Log($"No food mapped for spawn point '{spawnPoint.name}'");
                    }
                }
            }
        }
    }
}
