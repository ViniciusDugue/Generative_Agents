using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawnable Objects")]
    public GameObject foodPrefab;
    public GameObject enemyPrefab;
    public GameObject agentPrefab;

    [Header("Spawn Settings")]
    public int maxFood = 10;
    public int maxEnemies = 5;
    public int maxAgents = 5;
    public float spawnHeightOffset = 0.5f;

    [Header("Spawn Radii (Units)")]
    public float foodSpawnRadius = 5f;
    public float enemySpawnRadius = 10f;
    public float agentSpawnRadius = 7f;  // Optional, for visualization if needed.

    [Header("Dynamic Food Spawn Settings")]
    [Tooltip("Number of food spawn points to activate each day.")]
    public int dailyActiveFoodSpawnCount = 2;

    [Header("Dynamic Enemy Spawn Settings (Daytime Only)")]
    [Tooltip("Number of enemy spawn points to activate during the day.")]
    public int dailyActiveEnemySpawnCount = 2;

    // Spawned object lists.
    private List<GameObject> spawnedFood = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedAgents = new List<GameObject>();

    // Spawn point lists for food and enemy.
    private List<Transform> foodSpawnPoints = new List<Transform>();
    private List<Transform> enemySpawnPoints = new List<Transform>();

    // Active spawn points for food and enemy.
    private List<Transform> activeFoodSpawnPoints = new List<Transform>();
    private List<Transform> activeEnemySpawnPoints = new List<Transform>();
    private List<Transform> agentSpawnPoints = new List<Transform>();

    private MapMarkerManager markerManager;

    [Header("Time Manager Reference")]
    public TimeManager timeManager;

    // Track the previous day/night state.
    private bool lastIsDaytime;

    private void Awake()
    {
        markerManager = FindFirstObjectByType<MapMarkerManager>();
        FindSpawnPoints();

        if (timeManager == null)
        {
            timeManager = FindObjectOfType<TimeManager>();
        }
        if (timeManager != null)
        {
            lastIsDaytime = timeManager.IsDayTime;
        }
    }

    private void Start()
    {
        if (timeManager != null && timeManager.IsDayTime)
        {
            RandomizeFoodSpawnPoints();
            SpawnFoodAtActivePoints();

            RandomizeEnemySpawnPoints();
            SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

            // At scene start, spawn agents at the central hub.
            SpawnAgentsAtHub();
        }
    }

    private void Update()
    {
        if (timeManager != null)
        {
            if (timeManager.IsDayTime != lastIsDaytime)
            {
                lastIsDaytime = timeManager.IsDayTime;
                if (timeManager.IsDayTime)
                {
                    Debug.Log("Daytime started - repositioning habitat and spawning agents at the hub.");

                    // Reposition the habitat to a new agent spawn point.
                    GameObject habitatObj = GameObject.FindWithTag("habitat");
                    if (habitatObj != null)
                    {
                        Habitat habitat = habitatObj.GetComponent<Habitat>();
                        habitat.RepositionHabitat();
                    }
                    else
                    {
                        Debug.LogWarning("No habitat found to reposition.");
                    }

                    // Existing logic for food and enemy spawning...
                    RandomizeFoodSpawnPoints();
                    SpawnFoodAtActivePoints();

                    RandomizeEnemySpawnPoints();
                    DespawnObjects(spawnedEnemies);
                    SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

                    // Spawn agents from the new habitat location.
                    DespawnObjects(spawnedAgents);
                    SpawnAgentsAtHub();
                }
                else
                {
                    // Nighttime logic...
                    Debug.Log("Nighttime started.");
                    DespawnObjects(spawnedFood);
                    DisableFoodSpawnColliders();

                    DespawnObjects(spawnedEnemies);
                    ActivateAllEnemySpawnPoints();
                    SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

                    DespawnObjects(spawnedAgents);
                    DisableAgentSpawnColliders();
                }
            }
        }
    }

    // Finds spawn points for food and enemy objects based on their tags.
    private void FindSpawnPoints()
    {
        foodSpawnPoints.AddRange(FindObjectsWithTag("foodSpawn"));
        enemySpawnPoints.AddRange(FindObjectsWithTag("enemySpawn"));
        agentSpawnPoints.AddRange(FindObjectsWithTag("agentSpawn"));

        if (foodSpawnPoints.Count == 0) Debug.LogError("❌ No food spawn points found!");
        if (enemySpawnPoints.Count == 0) Debug.LogError("❌ No enemy spawn points found!");
        if (agentSpawnPoints.Count == 0) Debug.LogError("❌ No agent spawn points found!");
    }

    private List<Transform> FindObjectsWithTag(string tag)
    {
        List<Transform> points = new List<Transform>();
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in foundObjects)
        {
            points.Add(obj.transform);
        }
        return points;
    }

    // Randomly select active food spawn points.
    private void RandomizeFoodSpawnPoints()
    {
        activeFoodSpawnPoints.Clear();
        int countToSelect = Mathf.Min(dailyActiveFoodSpawnCount, foodSpawnPoints.Count);
        List<Transform> tempList = new List<Transform>(foodSpawnPoints);
        for (int i = 0; i < countToSelect; i++)
        {
            int index = Random.Range(0, tempList.Count);
            activeFoodSpawnPoints.Add(tempList[index]);
            tempList.RemoveAt(index);
        }
        foreach (Transform spawnPoint in foodSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                if (activeFoodSpawnPoints.Contains(spawnPoint))
                {
                    sc.radius = foodSpawnRadius;
                    sc.enabled = true;
                }
                else
                {
                    sc.enabled = false;
                }
            }
        }
        string log = "Day " + timeManager.Days + " - Active Food Spawn Points: ";
        foreach (Transform activePoint in activeFoodSpawnPoints)
        {
            log += (activePoint.name ?? activePoint.position.ToString()) + "; ";
        }
        Debug.Log(log);
    }

    // Randomizes active enemy spawn points (daytime only).
    private void RandomizeEnemySpawnPoints()
    {
        activeEnemySpawnPoints.Clear();
        int countToSelect = Mathf.Min(dailyActiveEnemySpawnCount, enemySpawnPoints.Count);
        List<Transform> tempList = new List<Transform>(enemySpawnPoints);
        for (int i = 0; i < countToSelect; i++)
        {
            int index = Random.Range(0, tempList.Count);
            activeEnemySpawnPoints.Add(tempList[index]);
            tempList.RemoveAt(index);
        }
        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                if (activeEnemySpawnPoints.Contains(spawnPoint))
                {
                    sc.radius = enemySpawnRadius;
                    sc.enabled = true;
                }
                else
                {
                    sc.enabled = false;
                }
            }
        }
        string log = "Day " + timeManager.Days + " - Active Enemy Spawn Points (Daytime): ";
        foreach (Transform activePoint in activeEnemySpawnPoints)
        {
            log += (activePoint.name ?? activePoint.position.ToString()) + "; ";
        }
        Debug.Log(log);
    }

    // Activates all enemy spawn points (for nighttime).
    private void ActivateAllEnemySpawnPoints()
    {
        activeEnemySpawnPoints.Clear();
        activeEnemySpawnPoints.AddRange(enemySpawnPoints);
        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.radius = enemySpawnRadius;
                sc.enabled = true;
            }
        }
        string log = "Day " + timeManager.Days + " - Active Enemy Spawn Points (Nighttime - ALL): ";
        foreach (Transform activePoint in activeEnemySpawnPoints)
        {
            log += (activePoint.name ?? activePoint.position.ToString()) + "; ";
        }
        Debug.Log(log);
    }

    // Spawns food objects from active food spawn points.
    private void SpawnFoodAtActivePoints()
    {
        DespawnObjects(spawnedFood);
        SpawnObjects(activeFoodSpawnPoints, foodPrefab, maxFood, spawnedFood, foodSpawnRadius);
    }

    // Generic spawn method.
    private void SpawnObjects(List<Transform> spawnPoints, GameObject prefab, int maxCount, List<GameObject> spawnedList, float spawnRadius)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found for " + prefab.name);
            return;
        }
        Debug.Log($"✅ Spawning {maxCount} {prefab.name}(s)");
        int totalSpawned = 0;
        foreach (Transform point in spawnPoints)
        {
            if (totalSpawned >= maxCount)
                break;

            int countPerPoint = Mathf.Min(maxCount - totalSpawned, 5); // Limit spawns per point
            for (int i = 0; i < countPerPoint; i++)
            {
                Vector3 randomPosition = GetRandomPositionAround(point.position, spawnRadius);
                GameObject newObj = Instantiate(prefab, randomPosition, Quaternion.identity);
                spawnedList.Add(newObj);
                totalSpawned++;

                if (prefab == agentPrefab)
                {
                    BehaviorManager behaviorManager = newObj.GetComponent<BehaviorManager>() 
                        ?? newObj.AddComponent<BehaviorManager>();
                    behaviorManager.InitializeAgent();

                    // Added AgentController component.
                    if (newObj.GetComponent<AgentController>() == null)
                    {
                        newObj.AddComponent<AgentController>();
                    }

                    // Removed MapEncoder logic here.

                    // Register marker for agent.
                    markerManager?.RegisterMarker(newObj);

                    // Notify the Client about the new agent.
                    Client client = FindFirstObjectByType<Client>();
                    client?.RegisterAgent(newObj);
                }
                else
                {
                    // Register marker for enemies and food.
                    markerManager?.RegisterMarker(newObj);
                }

                if (totalSpawned >= maxCount)
                    break;
            }
        }
    }

    // Spawns agents at the central hub (Habitat).
    private void SpawnAgentsAtHub()
    {
        GameObject habitatObj = GameObject.FindWithTag("habitat");
        if (habitatObj != null)
        {
            Habitat habitat = habitatObj.GetComponent<Habitat>();
            Vector3 hubPos = habitat.centralHubPoint.position;
            for (int i = 0; i < maxAgents; i++)
            {
                Vector3 spawnPos = hubPos + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                GameObject newAgent = Instantiate(agentPrefab, spawnPos, Quaternion.identity);
                spawnedAgents.Add(newAgent);

                // Ensure the agent has a BehaviorManager component.
                BehaviorManager behaviorManager = newAgent.GetComponent<BehaviorManager>() 
                                                ?? newAgent.AddComponent<BehaviorManager>();
                behaviorManager.InitializeAgent();

                // Added AgentController component.
                if (newAgent.GetComponent<AgentController>() == null)
                {
                    newAgent.AddComponent<AgentController>();
                }

                // Ensure the agent has a MapEncoder component.
                MapEncoder mapEncoder = newAgent.GetComponent<MapEncoder>() 
                                        ?? newAgent.AddComponent<MapEncoder>();
                // Locate the 2D map camera and assign it.
                GameObject mapCameraObj = GameObject.Find("AgentCamera");
                if(mapCameraObj != null)
                {
                    mapEncoder.mapCamera = mapCameraObj.GetComponent<Camera>();
                }
                else
                {
                    Debug.LogWarning("2DMapCamera not found!");
                }
                mapEncoder.serverUrl = "http://127.0.0.1:12345/map";

                // Register marker for this agent.
                markerManager?.RegisterMarker(newAgent);

                // Notify the Client about the new agent.
                Client client = FindFirstObjectByType<Client>();
                client?.RegisterAgent(newAgent);
            }
        }
        else
        {
            Debug.LogWarning("No central hub (Habitat) found for spawning agents.");
        }
    }

    // Returns a random position within a circle around a center.
    private Vector3 GetRandomPositionAround(Vector3 center, float radius)
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2);
        float randomDistance = Random.Range(0f, radius);
        float xOffset = Mathf.Cos(randomAngle) * randomDistance;
        float zOffset = Mathf.Sin(randomAngle) * randomDistance;
        return new Vector3(center.x + xOffset, center.y + spawnHeightOffset, center.z + zOffset);
    }

    // Despawns all objects in the provided list.
    private void DespawnObjects(List<GameObject> spawnedList)
    {
        foreach (GameObject obj in spawnedList)
        {
            if (obj != null)
            {
                markerManager?.RemoveMarker(obj);
                Destroy(obj);
            }
        }
        spawnedList.Clear();
    }

    // Disables colliders on food spawn points.
    private void DisableFoodSpawnColliders()
    {
        foreach (Transform spawnPoint in foodSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.enabled = false;
            }
        }
    }

    // Disables colliders on enemy spawn points.
    private void DisableEnemySpawnColliders()
    {
        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.enabled = false;
            }
        }
    }

    // Optionally disable agent spawn colliders if used.
    private void DisableAgentSpawnColliders()
    {
        // Not needed here since agents are spawned from the central hub.
    }
}
