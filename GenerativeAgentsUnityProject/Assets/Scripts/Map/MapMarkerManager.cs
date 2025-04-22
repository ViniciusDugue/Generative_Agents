using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;
    // public RectTransform mapContainer;
    [Header("Map Prefab")]
    public GameObject mapPrefab;

    [Header("Marker Prefabs")]
    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

    public GameObject foodSpawnPointMarkerPrefab;

    // [Header("Agent Map Settings")]
    // [Tooltip("Reference to the PersonalMap component used for agent discovery (used for the agent map).")]
    // public PersonalMap agentPersonalMap; // Assign via Inspector if the agent map should filter markers

    // These will hold our map containers
    private RectTransform userMapContainer;
    private RectTransform agentMapContainer;
    private readonly Vector3 defaultAgentMapLocation = new Vector3(-100f, 0f, -50f);

    // Dictionaries to track markers for each map type.
    private Dictionary<GameObject, GameObject> userMarkers = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, GameObject> agentMarkers = new Dictionary<GameObject, GameObject>();

    // Hardcoded map resolution - set to (700,700) or (225,225) as needed
    public Vector2 mapResolution = new Vector2(700, 700);

    // Agent Information to be Marked
    [HideInInspector]
    public GameObject[] agentList;
    // Information about all agents knowledge about the environment (stuff to be marked)
    private static List<AgentMapInfo> allAgentMapInfos = new List<AgentMapInfo>(); 
    public Dictionary<string, GameObject> agentMapDict = new Dictionary<string, GameObject>();
    //event that tracks when the agentMapDict is fully updated
    public static event Action<Dictionary<string, GameObject>> mapDictFullyBuilt;

    // Dictionary to keep track of markers by their associated tracked objects.
    // private Dictionary<GameObject, GameObject> markers = new Dictionary<GameObject, GameObject>();

    // // Scan interval to catch any unregistered spawned objects.
    // private float scanInterval = 1f;
    // private float scanTimer = 0f;

    private void Start()
    {
        registerAgents();
        createAgentMaps();

        // Get all RectTransform components in this object's hierarchy, including nested ones
        RectTransform[] allMaps = GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rt in allMaps)
        {
            // Skip the parent (MapController) itself

            if (rt.gameObject == gameObject) continue;
            if (rt.CompareTag("usermap"))
            {
                userMapContainer = rt;
                Debug.Log("User map container found: " + rt.name);
            }
            else if (rt.CompareTag("agentmap"))
            {
                agentMapContainer = rt;
                Debug.Log("Agent map container found: " + rt.name);
            }
        }
        if (userMapContainer == null)
            Debug.LogWarning("No child with tag 'usermap' found in MapController.");
        if (agentMapContainer == null)
            Debug.LogWarning("No child with tag 'agentmap' found in MapController.");
    }

    // Subscribe to marker events.
    private void OnEnable()
    {
        MarkerEventManager.OnMarkerSpawned += OnMarkerSpawnedHandler;
        MarkerEventManager.OnMarkerRemoved += OnMarkerRemovedHandler;
    }

    // Unsubscribe from marker events.
    private void OnDisable()
    {
        MarkerEventManager.OnMarkerSpawned -= OnMarkerSpawnedHandler;
        MarkerEventManager.OnMarkerRemoved -= OnMarkerRemovedHandler;
    }

    void registerAgents() {
        // Register all Personal Maps for each Agent
        agentList = GameObject.FindGameObjectsWithTag("agent");
        if (agentList.Length == 0) 
        {
            Debug.LogWarning("No Agents Found"); 
            return; // Exit the method if no agents are found;
        }
        
        foreach(GameObject agent in agentList) {
            var info = agent.GetComponent<AgentMapInfo>();
            BehaviorManager bm = agent.GetComponent<BehaviorManager>();
            if (info != null) {
                Debug.Log("Adding Personal Map for " + agent.name);
                allAgentMapInfos.Add(info);
                
            }
        }
    }

    void createAgentMaps() {
    // How much to shift each successive map along X (in UI units)
    Vector3 rectMapOffset = new Vector3(-50, 0, 0);
    int idx = 0;

    // Loop over every agent in the scene (assumes agentList is already populated)
    for (int i = 0; i < agentList.Length; i++) {
        // Instantiate a copy of the mapPrefab as a child of this GameObject
        GameObject agentMap = Instantiate(mapPrefab, this.transform, false);
        agentMap.name = $"AgentMap-{idx}";

        // Calculate where to place it: start from a base location, then offset by
        // rectMapOffset multiplied by the loop index i
        Vector2 newPos = defaultAgentMapLocation + rectMapOffset * i;

        // Apply the computed position to the RectTransform's anchoredPosition
        agentMap.GetComponent<RectTransform>().anchoredPosition = newPos;

        // Store the new map in a dictionary for quick lookup by name later
        agentMapDict.Add(agentMap.name, agentMap);

        idx++;
    }
    mapDictFullyBuilt?.Invoke(agentMapDict); // Notify listeners that the map dictionary is fully built.);
}


    // Called when a marker spawn event is raised.
    private void OnMarkerSpawnedHandler(GameObject trackedObject, MarkerEventManager.MarkerType markerType)
    {
        // For the user map, always add the marker.
        if (userMapContainer != null && trackedObject != null && !userMarkers.ContainsKey(trackedObject))
        {
            GameObject prefab = GetPrefabForMarker(markerType);
            if (prefab != null)
            {
                GameObject marker = Instantiate(prefab, userMapContainer);
                marker.name = trackedObject.name + "_UserMarker";
                userMarkers[trackedObject] = marker;
            }
        }

        // For the agent map, we differentiate based on marker type.
        if (agentMapContainer != null && trackedObject != null && !agentMarkers.ContainsKey(trackedObject))
        {
            // For food and food spawn markers, check the agent's personal memory.
            if (markerType == MarkerEventManager.MarkerType.Food || markerType == MarkerEventManager.MarkerType.FoodSpawn)
            {
                bool discovered = false;
                foreach (GameObject agent in agentList)
                {
                    AgentMapInfo agentMapInfo = agent.GetComponent<AgentMapInfo>();
                    foreach (AgentMapInfo.MarkerData data in agentMapInfo.knownMarkers)
                    {
                        // Only register if at least one agent has discovered this object.
                        if (data.markerType == markerType && data.discoveredObject == trackedObject)
                        {
                            discovered = true;
                            break;
                        }
                    }
                    if (discovered) break;
                }
                if (discovered)
                {
                    GameObject prefab = GetPrefabForMarker(markerType);
                    if (prefab != null)
                    {
                        GameObject marker = Instantiate(prefab, agentMapContainer);
                        marker.name = trackedObject.name + "_AgentMarker";
                        agentMarkers[trackedObject] = marker;
                    }
                }
            }
            // For enemy and agent markers, instantiate immediately.
            else
            {
                GameObject prefab = GetPrefabForMarker(markerType);
                if (prefab != null)
                {
                    GameObject marker = Instantiate(prefab, agentMapContainer);
                    marker.name = trackedObject.name + "_AgentMarker";
                    agentMarkers[trackedObject] = marker;
                }
            }
        }
    }

    // Helper method to select the correct prefab based on marker type.
    private GameObject GetPrefabForMarker(MarkerEventManager.MarkerType markerType)
    {
        switch (markerType)
        {
            case MarkerEventManager.MarkerType.Agent:
                return agentMarkerPrefab;
            case MarkerEventManager.MarkerType.Enemy:
                return enemyMarkerPrefab;
            case MarkerEventManager.MarkerType.Food:
                return foodMarkerPrefab;
            case MarkerEventManager.MarkerType.FoodSpawn:
                return foodSpawnPointMarkerPrefab;
            default:
                return null;
        }
    }

    // Called when a marker removal event is raised.
    private void OnMarkerRemovedHandler(GameObject trackedObject)
    {
        if (trackedObject == null) return;
        if (userMarkers.ContainsKey(trackedObject))
        {
            Destroy(userMarkers[trackedObject]);
            userMarkers.Remove(trackedObject);
        }
        if (agentMarkers.ContainsKey(trackedObject))
        {
            Destroy(agentMarkers[trackedObject]);
            agentMarkers.Remove(trackedObject);
        }
    }


    // void Start()
    // {
    //     InitializeMarkers();
    // }

    void Update()
    {
        // Update positions of markers for both maps.
        UpdateMarkerPositions(userMarkers, userMapContainer);
        UpdateMarkerPositions(agentMarkers, agentMapContainer);
    }

    // private void InitializeMarkers()
    // {
    //     // This will add markers for any objects with the given tags that don't already have a marker.
    //     AddMarkers(GameObject.FindGameObjectsWithTag("agent"), agentMarkerPrefab);
    //     AddMarkers(GameObject.FindGameObjectsWithTag("enemyAgent"), enemyMarkerPrefab);
    // }

    // private void AddMarkers(GameObject[] objects, GameObject prefab)
    // {
    //     foreach (GameObject obj in objects)
    //     {
    //         if (obj != null && !markers.ContainsKey(obj))
    //         {
    //             AddMarker(obj, prefab);
    //         }
    //     }
    // }

    // private void AddMarker(GameObject obj, GameObject prefab)
    // {
    //     GameObject marker = Instantiate(prefab, mapContainer);
    //     marker.name = obj.name + "_Marker";
    //     markers[obj] = marker;
    // }

    // public void RegisterMarker(GameObject obj)
    // {
    //     if (obj.CompareTag("agent"))
    //     {
    //         AddMarker(obj, agentMarkerPrefab);
    //     }
    //     else if (obj.CompareTag("enemyAgent"))
    //     {
    //         AddMarker(obj, enemyMarkerPrefab);
    //     }
    //     else if (obj.CompareTag("food"))
    //     {
    //         AddMarker(obj, foodMarkerPrefab);
    //     }
    // }

    // public void RemoveMarker(GameObject obj)
    // {
    //     if (markers.ContainsKey(obj))
    //     {
    //         Destroy(markers[obj]);
    //         markers.Remove(obj);
    //     }
    // }

    // NEW: Method to register a discovered spawn point.
    // public void RegisterDiscoveredSpawnPoint(GameObject spawnPoint, string spawnType)
    // {
    //     // Only register if a marker for this spawn point doesn't already exist.
    //     if (!markers.ContainsKey(spawnPoint))
    //     {
    //         GameObject marker = null;
    //         if (spawnType == "foodSpawn")
    //         {
    //             // Instantiate without specifying a parent, then set the parent explicitly.
    //             marker = Instantiate(foodSpawnPointMarkerPrefab);
    //             if (mapContainer != null)
    //             {
    //                 marker.transform.SetParent(mapContainer, false);
    //             }
    //             else
    //             {
    //                 Debug.LogError("mapContainer is not assigned in MapMarkerManager!");
    //             }
    //         }
            
    //         if (marker != null)
    //         {
    //             marker.name = spawnPoint.name + "_SpawnMarker";
    //             markers[spawnPoint] = marker;
    //             Debug.Log($"Registered discovered spawn point marker for '{spawnPoint.name}' with gametag '{spawnType}'. Parent: {marker.transform.parent.name}");
    //         }
    //         else
    //         {
    //             Debug.LogWarning("No marker prefab found for spawn type: " + spawnType);
    //         }
    //     }
    //     else
    //     {
    //         Debug.Log($"Spawn point '{spawnPoint.name}' is already registered.");
    //     }
    // }

    // public void RegisterDiscoveredFood(GameObject foodObject)
    // {
    //     // Only register if a marker for this food isn't already present.
    //     if (!markers.ContainsKey(foodObject))
    //     {
    //         GameObject marker = Instantiate(foodMarkerPrefab, mapContainer);
    //         marker.name = foodObject.name + "_FoodMarker";
    //         markers[foodObject] = marker;
    //         Debug.Log($"Registered discovered food marker for '{foodObject.name}'.");
    //     }
    //     else
    //     {
    //         Debug.Log($"Food '{foodObject.name}' is already registered.");
    //     }
    // }

    // Updates marker positions based on tracked object's world position.
    private void UpdateMarkerPositions(Dictionary<GameObject, GameObject> markerDict, RectTransform container)
    {
        // Choose resolution based on which map is being updated.
        Vector2 resolution = mapResolution; // Default for user map.
        if (container == agentMapContainer)
        {
            resolution = new Vector2(225, 225); // Agent map resolution.
        }

        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in markerDict)
        {
            GameObject trackedObject = kvp.Key;
            GameObject marker = kvp.Value;

            if (trackedObject == null)
            {
                toRemove.Add(trackedObject);
                Destroy(marker);
                continue;
            }

            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);
            marker.SetActive(viewportPos.z >= 0);

            Vector2 localPos = new Vector2(
                (viewportPos.x - 0.5f) * resolution.x,
                (viewportPos.y - 0.5f) * resolution.y
            );

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.anchoredPosition = localPos;
            }
            else
            {
                Debug.LogWarning($"Marker '{marker.name}' is missing a RectTransform.");
            }

            // Optionally update coordinate text.
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

        foreach (GameObject obj in toRemove)
        {
            markerDict.Remove(obj);
        }
    }
}
