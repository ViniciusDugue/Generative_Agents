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

    private List<GameObject> spawnedFood = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedAgents = new List<GameObject>();

    private List<Transform> foodSpawnPoints = new List<Transform>();
    private List<Transform> enemySpawnPoints = new List<Transform>();
    private List<Transform> agentSpawnPoints = new List<Transform>();

    private MapMarkerManager markerManager;

    [Header("Time Manager Reference")]
    // Assign via inspector or let the script find it automatically.
    public TimeManager timeManager;

    // Keep track of the last known day/night state.
    private bool lastIsDaytime;

    private void Awake()
    {
        markerManager = FindObjectOfType<MapMarkerManager>();
        FindSpawnPoints();

        // Try to locate the TimeManager if it wasn't assigned.
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
        // Spawn initial objects. Food is spawned only if it's daytime.
        if (timeManager != null && timeManager.IsDayTime)
        {
            SpawnObjects(foodSpawnPoints, foodPrefab, maxFood, spawnedFood, foodSpawnRadius);
        }
        SpawnObjects(enemySpawnPoints, enemyPrefab, maxEnemies, spawnedEnemies, enemySpawnRadius);
        SpawnObjects(agentSpawnPoints, agentPrefab, maxAgents, spawnedAgents, agentSpawnRadius);
    }

    private void Update()
    {
        // Poll the TimeManager's IsDayTime state and compare with our last stored state.
        if (timeManager != null)
        {
            if (timeManager.IsDayTime != lastIsDaytime)
            {
                lastIsDaytime = timeManager.IsDayTime;
                if (lastIsDaytime)
                {
                    Debug.Log("Daytime started - Agents can gather food.");
                    // Respawn food at the start of the day.
                    SpawnObjects(foodSpawnPoints, foodPrefab, maxFood, spawnedFood, foodSpawnRadius);
                }
                else
                {
                    Debug.Log("Nighttime started - Agents return to base.");
                    // Despawn food at night.
                    DespawnObjects(spawnedFood);
                }
            }
        }
    }

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

    private void SpawnObjects(List<Transform> spawnPoints, GameObject prefab, int maxCount, List<GameObject> spawnedList, float spawnRadius)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"No spawn points found for {prefab.name}");
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            for (int i = 0; i < maxCount; i++)
            {
                Vector3 randomPosition = GetRandomPositionAround(point.position, spawnRadius);
                GameObject newObj = Instantiate(prefab, randomPosition, Quaternion.identity);
                spawnedList.Add(newObj);

                // Register new object with minimap if available.
                if (markerManager != null)
                {
                    markerManager.RegisterMarker(newObj);
                }
            }
        }
    }

    private Vector3 GetRandomPositionAround(Vector3 center, float radius)
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2);
        float randomDistance = Random.Range(0f, radius);
        float xOffset = Mathf.Cos(randomAngle) * randomDistance;
        float zOffset = Mathf.Sin(randomAngle) * randomDistance;

        return new Vector3(center.x + xOffset, center.y + spawnHeightOffset, center.z + zOffset);
    }

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

    // Optional helper method.
    public bool IsDaytime()
    {
        return timeManager != null ? timeManager.IsDayTime : true;
    }
}
