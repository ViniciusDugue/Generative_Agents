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
    public float agentSpawnRadius = 7f;

    [Header("Dynamic Food Spawn Settings")]
    [Tooltip("Number of food spawn points to activate each day.")]
    public int dailyActiveFoodSpawnCount = 2;

    [Header("Dynamic Enemy Spawn Settings (Daytime Only)")]
    [Tooltip("Number of enemy spawn points to activate during the day.")]
    public int dailyActiveEnemySpawnCount = 2;

    [Header("Dynamic Agent Spawn Settings")]
    [Tooltip("Number of agent spawn points to activate each day.")]
    public int dailyActiveAgentSpawnCount = 2;

    // Spawned object lists.
    private List<GameObject> spawnedFood = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedAgents = new List<GameObject>();

    // All spawn points from the scene.
    private List<Transform> foodSpawnPoints = new List<Transform>();
    private List<Transform> enemySpawnPoints = new List<Transform>();
    private List<Transform> agentSpawnPoints = new List<Transform>();

    // Active spawn points selected for the current day.
    private List<Transform> activeFoodSpawnPoints = new List<Transform>();
    private List<Transform> activeEnemySpawnPoints = new List<Transform>();
    private List<Transform> activeAgentSpawnPoints = new List<Transform>();

    private MapMarkerManager markerManager;

    [Header("Time Manager Reference")]
    public TimeManager timeManager;

    // For tracking day/night transitions.
    private bool lastIsDaytime;

    private void Awake()
    {
        markerManager = FindObjectOfType<MapMarkerManager>();
        FindSpawnPoints();

        // Locate the TimeManager if it wasn’t assigned.
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
        // When the scene starts and it's daytime, randomize and spawn for all categories.
        if (timeManager != null && timeManager.IsDayTime)
        {
            RandomizeFoodSpawnPoints();
            SpawnFoodAtActivePoints();

            RandomizeEnemySpawnPoints();
            SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

            RandomizeAgentSpawnPoints();
            SpawnObjects(activeAgentSpawnPoints, agentPrefab, maxAgents, spawnedAgents, agentSpawnRadius);
        }
    }

    private void Update()
    {
        if (timeManager != null)
        {
            // Check for day/night state change.
            if (timeManager.IsDayTime != lastIsDaytime)
            {
                lastIsDaytime = timeManager.IsDayTime;
                if (timeManager.IsDayTime)
                {
                    Debug.Log("Daytime started - Activating randomized spawn points for food, enemy, and agent.");

                    // Food spawn points: randomize.
                    RandomizeFoodSpawnPoints();
                    SpawnFoodAtActivePoints();

                    // Enemy spawn points: randomize.
                    RandomizeEnemySpawnPoints();
                    DespawnObjects(spawnedEnemies);
                    SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

                    // Agent spawn points: randomize.
                    RandomizeAgentSpawnPoints();
                    DespawnObjects(spawnedAgents);
                    SpawnObjects(activeAgentSpawnPoints, agentPrefab, maxAgents, spawnedAgents, agentSpawnRadius);
                }
                else
                {
                    Debug.Log("Nighttime started - Activating ALL enemy spawn points.");
                    // Food: disable food spawns at night.
                    DespawnObjects(spawnedFood);
                    DisableFoodSpawnColliders();

                    // Enemies: activate all spawn points.
                    DespawnObjects(spawnedEnemies);
                    ActivateAllEnemySpawnPoints();
                    SpawnObjects(activeEnemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);

                    // Optionally disable agent spawns at night (if desired).
                    DespawnObjects(spawnedAgents);
                    DisableAgentSpawnColliders();
                }
            }
        }
    }

    // Finds spawn points based on tags.
    private void FindSpawnPoints()
    {
        foodSpawnPoints.AddRange(FindObjectsWithTag("foodSpawn"));
        enemySpawnPoints.AddRange(FindObjectsWithTag("enemySpawn"));
        agentSpawnPoints.AddRange(FindObjectsWithTag("agentSpawn"));
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

    // Randomizes active food spawn points.
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

    // Randomizes active enemy spawn points (for daytime only).
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

    // Activates ALL enemy spawn points (for nighttime).
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

    // Randomizes active agent spawn points.
    private void RandomizeAgentSpawnPoints()
    {
        activeAgentSpawnPoints.Clear();
        int countToSelect = Mathf.Min(dailyActiveAgentSpawnCount, agentSpawnPoints.Count);
        List<Transform> tempList = new List<Transform>(agentSpawnPoints);

        for (int i = 0; i < countToSelect; i++)
        {
            int index = Random.Range(0, tempList.Count);
            activeAgentSpawnPoints.Add(tempList[index]);
            tempList.RemoveAt(index);
        }

        foreach (Transform spawnPoint in agentSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                if (activeAgentSpawnPoints.Contains(spawnPoint))
                {
                    sc.radius = agentSpawnRadius;
                    sc.enabled = true;
                }
                else
                {
                    sc.enabled = false;
                }
            }
        }

        string log = "Day " + timeManager.Days + " - Active Agent Spawn Points: ";
        foreach (Transform activePoint in activeAgentSpawnPoints)
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

    // Generic spawn method for any type.
    private void SpawnObjects(List<Transform> spawnPoints, GameObject prefab, int maxCount, List<GameObject> spawnedList, float spawnRadius)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found for " + prefab.name);
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            for (int i = 0; i < maxCount; i++)
            {
                Vector3 randomPosition = GetRandomPositionAround(point.position, spawnRadius);
                GameObject newObj = Instantiate(prefab, randomPosition, Quaternion.identity);
                spawnedList.Add(newObj);
                if (markerManager != null)
                {
                    markerManager.RegisterMarker(newObj);
                }
            }
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

    // Destroys objects in the provided list.
    private void DespawnObjects(List<GameObject> spawnedList)
    {
        foreach (GameObject obj in spawnedList)
        {
            if (obj != null)
            {
                if (markerManager != null)
                {
                    markerManager.RemoveMarker(obj);
                }
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

    // Disables colliders on agent spawn points.
    private void DisableAgentSpawnColliders()
    {
        foreach (Transform spawnPoint in agentSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.enabled = false;
            }
        }
    }

    // Draws gizmos for visualizing spawn points:
    // - Blue for agent spawn points
    // - Red for enemy spawn points
    // - Green for food spawn points
    private void OnDrawGizmos()
    {
        // Populate spawn point lists if they are empty.
        if (agentSpawnPoints == null || agentSpawnPoints.Count == 0)
        {
            GameObject[] agentObjs = GameObject.FindGameObjectsWithTag("agentSpawn");
            agentSpawnPoints = new List<Transform>();
            foreach (GameObject obj in agentObjs)
            {
                agentSpawnPoints.Add(obj.transform);
            }
        }
        if (enemySpawnPoints == null || enemySpawnPoints.Count == 0)
        {
            GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("enemySpawn");
            enemySpawnPoints = new List<Transform>();
            foreach (GameObject obj in enemyObjs)
            {
                enemySpawnPoints.Add(obj.transform);
            }
        }
        if (foodSpawnPoints == null || foodSpawnPoints.Count == 0)
        {
            GameObject[] foodObjs = GameObject.FindGameObjectsWithTag("foodSpawn");
            foodSpawnPoints = new List<Transform>();
            foreach (GameObject obj in foodObjs)
            {
                foodSpawnPoints.Add(obj.transform);
            }
        }

        // Draw agent spawn points in blue.
        Gizmos.color = Color.blue;
        foreach (Transform spawnPoint in agentSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            float radius = agentSpawnRadius;
            if (sc != null)
            {
                radius = sc.radius;
            }
            Gizmos.DrawWireSphere(spawnPoint.position, radius);
        }

        // Draw enemy spawn points in red.
        Gizmos.color = Color.red;
        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            float radius = enemySpawnRadius;
            if (sc != null)
            {
                radius = sc.radius;
            }
            Gizmos.DrawWireSphere(spawnPoint.position, radius);
        }

        // Draw food spawn points in green.
        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in foodSpawnPoints)
        {
            SphereCollider sc = spawnPoint.GetComponent<SphereCollider>();
            float radius = foodSpawnRadius;
            if (sc != null)
            {
                radius = sc.radius;
            }
            Gizmos.DrawWireSphere(spawnPoint.position, radius);
        }
    }
}
