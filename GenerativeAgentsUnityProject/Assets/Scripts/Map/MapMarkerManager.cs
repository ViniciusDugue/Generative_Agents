using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    // // Nested class to store marker pairs for two different maps.
    // public class MarkerPair
    // {
    //     public GameObject agentMarker;
    //     public GameObject userMarker;

    //     public MarkerPair(GameObject agentMarker, GameObject userMarker)
    //     {
    //         this.agentMarker = agentMarker;
    //         this.userMarker = userMarker;
    //     }
    // }

    public Camera mapCamera;
    public RectTransform mapContainer;

    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

    public GameObject foodSpawnPointMarkerPrefab;

    // Hardcoded map resolution - set to (700,700) or (225,225) as needed
    public Vector2 mapResolution = new Vector2(700, 700);

    private Dictionary<GameObject, GameObject> markers = new Dictionary<GameObject, GameObject>();

    // Scan interval to catch any unregistered spawned objects.
    private float scanInterval = 1f;
    private float scanTimer = 0f;

    void Start()
    {
        InitializeMarkers();
    }

    void Update()
    {
        UpdateMarkerPositions();

        // Periodically check for new objects that haven't been registered.
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            InitializeMarkers();
        }
    }

    private void InitializeMarkers()
    {
        // This will add markers for any objects with the given tags that don't already have a marker.
        AddMarkers(GameObject.FindGameObjectsWithTag("agent"), agentMarkerPrefab);
        AddMarkers(GameObject.FindGameObjectsWithTag("enemyAgent"), enemyMarkerPrefab);
    }

    private void AddMarkers(GameObject[] objects, GameObject prefab)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null && !markers.ContainsKey(obj))
            {
                AddMarker(obj, prefab);
            }
        }
    }

    private void AddMarker(GameObject obj, GameObject prefab)
    {
        GameObject marker = Instantiate(prefab, mapContainer);
        marker.name = obj.name + "_Marker";
        markers[obj] = marker;
    }

    public void RegisterMarker(GameObject obj)
    {
        if (obj.CompareTag("agent"))
        {
            AddMarker(obj, agentMarkerPrefab);
        }
        else if (obj.CompareTag("enemyAgent"))
        {
            AddMarker(obj, enemyMarkerPrefab);
        }
        else if (obj.CompareTag("food"))
        {
            AddMarker(obj, foodMarkerPrefab);
        }
    }

    public void RemoveMarker(GameObject obj)
    {
        if (markers.ContainsKey(obj))
        {
            Destroy(markers[obj]);
            markers.Remove(obj);
        }
    }

    // NEW: Method to register a discovered spawn point.
    public void RegisterDiscoveredSpawnPoint(GameObject spawnPoint, string spawnType)
    {
        // Only register if a marker for this spawn point doesn't already exist.
        if (!markers.ContainsKey(spawnPoint))
        {
            GameObject marker = null;
            if (spawnType == "foodSpawn")
            {
                // Instantiate without specifying a parent, then set the parent explicitly.
                marker = Instantiate(foodSpawnPointMarkerPrefab);
                if (mapContainer != null)
                {
                    marker.transform.SetParent(mapContainer, false);
                }
                else
                {
                    Debug.LogError("mapContainer is not assigned in MapMarkerManager!");
                }
            }
            
            if (marker != null)
            {
                marker.name = spawnPoint.name + "_SpawnMarker";
                markers[spawnPoint] = marker;
                Debug.Log($"Registered discovered spawn point marker for '{spawnPoint.name}' with gametag '{spawnType}'. Parent: {marker.transform.parent.name}");
            }
            else
            {
                Debug.LogWarning("No marker prefab found for spawn type: " + spawnType);
            }
        }
        else
        {
            Debug.Log($"Spawn point '{spawnPoint.name}' is already registered.");
        }
    }

    public void RegisterDiscoveredFood(GameObject foodObject)
    {
        // Only register if a marker for this food isn't already present.
        if (!markers.ContainsKey(foodObject))
        {
            GameObject marker = Instantiate(foodMarkerPrefab, mapContainer);
            marker.name = foodObject.name + "_FoodMarker";
            markers[foodObject] = marker;
            Debug.Log($"Registered discovered food marker for '{foodObject.name}'.");
        }
        else
        {
            Debug.Log($"Food '{foodObject.name}' is already registered.");
        }
    }

    private void UpdateMarkerPositions()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var pair in markers)
        {
            GameObject trackedObject = pair.Key;
            GameObject marker = pair.Value;

            // If the object has been destroyed, mark it for removal.
            if (trackedObject == null)
            {
                toRemove.Add(trackedObject);
                Destroy(marker);
                continue;
            }

            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);
            marker.SetActive(viewportPos.z >= 0);

            // Calculate local position using the hardcoded resolution.
            Vector2 localPos = new Vector2(
                (viewportPos.x - 0.5f) * mapResolution.x,
                (viewportPos.y - 0.5f) * mapResolution.y
            );
            marker.GetComponent<RectTransform>().anchoredPosition = localPos;

            // Optionally update coordinate text under the marker.
            Transform textTransform = marker.transform.Find("CoordinateText");
            if (textTransform != null)
            {
                TMP_Text tmpText = textTransform.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = $"({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})";
                }
            }
        }

        // Remove markers for objects that no longer exist.
        foreach (GameObject obj in toRemove)
        {
            markers.Remove(obj);
        }
    }
}
