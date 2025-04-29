<<<<<<< HEAD
=======
using System;
using System.Collections;
>>>>>>> main
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
<<<<<<< HEAD
    public Camera mapCamera;
    public RectTransform mapContainer;

    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

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
        AddMarkers(GameObject.FindGameObjectsWithTag("food"), foodMarkerPrefab);
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
=======
    // LEGACY EVENT for MapEncoder or any other subscriber
    public static event Action<Dictionary<string, GameObject>> mapDictFullyBuilt;

    [Header("Camera & Map Prefab")]
    public Camera    mapCamera;
    public GameObject mapPrefab;

    [Header("Marker Prefabs")]
    public GameObject agentMarkerPrefab;
    public GameObject allyMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject enemySpawnPointMarkerPrefab;
    public GameObject foodMarkerPrefab;
    public GameObject foodSpawnPointMarkerPrefab;
    public GameObject activeFoodSpawnPointMarkerPrefab;

    // Reference for spawn‐point data
    private SpawnManager spawnManager;

    [Header("Map Resolutions")]
    public Vector2 userMapResolution  = new Vector2(700, 700);
    public Vector2 agentMapResolution = new Vector2(225, 225);

    [HideInInspector] public GameObject[] agentList;

    // Keeps each AgentMapInfo exactly once
    private static List<AgentMapInfo> allAgentMapInfos = new List<AgentMapInfo>();

    // One UI container per agent
    private Dictionary<AgentMapInfo, RectTransform> agentMapContainers =
        new Dictionary<AgentMapInfo, RectTransform>();

    private RectTransform userMapContainer;

    // Tracks spawned markers
    private Dictionary<GameObject, GameObject>        userMarkers  =
        new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, List<GameObject>> agentMarkers =
        new Dictionary<GameObject, List<GameObject>>();

    // Where to start placing your agent maps
    private readonly Vector3 defaultAgentMapLocation = new Vector3(-100f, 0f, -50f);

    private void Awake()
    {
        MarkerEventManager.OnMarkerSpawned += OnMarkerSpawnedHandler;
        MarkerEventManager.OnMarkerRemoved += OnMarkerRemovedHandler;
        spawnManager = FindObjectOfType<SpawnManager>();
    }

    private void Start()
    {
        // delay map setup by one frame so SpawnManager.Start() has created the agents
        StartCoroutine(DelayedMapSetup());
    }

    private IEnumerator DelayedMapSetup()
    {
        yield return null;
        registerAgents();
        createAgentMaps();

        // 2) Find the user-map container if you want user markers too
        if (userMapContainer == null)
        {
            foreach (var rt in GetComponentsInChildren<RectTransform>(true))
                if (rt.CompareTag("usermap"))
                    userMapContainer = rt;
            if (userMapContainer == null)
                Debug.LogWarning("No child tagged 'usermap' found for user markers.");
        }
        
        // 3) “Re-fire” every existing spawn so markers get added
        //    – Agents:
        foreach (var agentGO in agentList)
            OnMarkerSpawnedHandler(agentGO, MarkerEventManager.MarkerType.Agent);

        //    – Enemies:
        foreach (var enemyGO in GameObject.FindGameObjectsWithTag("enemyAgent"))
            OnMarkerSpawnedHandler(enemyGO, MarkerEventManager.MarkerType.Enemy);

    }

    private void OnDestroy()
    {
        MarkerEventManager.OnMarkerSpawned -= OnMarkerSpawnedHandler;
        MarkerEventManager.OnMarkerRemoved -= OnMarkerRemovedHandler;
    }

    /// <summary>
    /// Finds all agents and populates allAgentMapInfos once.
    /// </summary>
    void registerAgents()
    {
        agentList = GameObject.FindGameObjectsWithTag("agent");
        allAgentMapInfos.Clear();
        foreach (GameObject agent in agentList)
        {
            AgentMapInfo info = agent.GetComponent<AgentMapInfo>();
            if (info != null)
                allAgentMapInfos.Add(info);
        }
    }

    /// <summary>
    /// Locates user map container, creates one mapPrefab per agent,
    /// caches each child tagged 'agentmap', and fires the legacy event.
    /// </summary>
    void createAgentMaps()
    {
        // 1) Find the user map container
        userMapContainer = null;
        foreach (RectTransform rt in GetComponentsInChildren<RectTransform>(true))
        {
            if (rt.CompareTag("usermap"))
            {
                userMapContainer = rt;
                break;
            }
        }
        if (userMapContainer == null)
            Debug.LogWarning("No child tagged 'usermap' found in MapController.");

        // Prepare the legacy map dictionary
        var mapDict = new Dictionary<string, GameObject>();

        // 2) Spawn & cache agent maps
        agentMapContainers.Clear();
        Vector3 basePos = defaultAgentMapLocation;
        Vector3 offset  = new Vector3(-50f, 0f, 0f);

        for (int i = 0; i < agentList.Length; i++)
        {
            GameObject agentGO = agentList[i];
            AgentMapInfo info  = agentGO.GetComponent<AgentMapInfo>();
            if (info == null) continue;

            // a) Instantiate the map prefab
            GameObject mapInstance = Instantiate(mapPrefab, transform, false);
            var bm = agentGO.GetComponent<BehaviorManager>();
            int id = bm != null ? bm.agentID : (i + 1);
            mapInstance.name = $"AgentMap-{id}";
            mapInstance.GetComponent<RectTransform>().anchoredPosition = basePos + offset * i;

            // b) Cache its internal container
            RectTransform agentContainer = null;
            foreach (RectTransform child in mapInstance.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.CompareTag("agentmap"))
                {
                    agentContainer = child;
                    break;
                }
            }
            if (agentContainer == null)
            {
                Debug.LogError("Map prefab missing a child tagged 'agentmap'.");
                Destroy(mapInstance);
                continue;
            }
            agentMapContainers[info] = agentContainer;

            // c) Add to legacy dictionary
            mapDict[mapInstance.name] = mapInstance;
        }

        // 3) Fire legacy event for any subscribers
        mapDictFullyBuilt?.Invoke(mapDict);
    }

    private void OnMarkerSpawnedHandler(GameObject trackedObject, MarkerEventManager.MarkerType markerType)
    {
        if (trackedObject == null) 
            return;

        // ── USER MAP ── (unchanged) 
        if (userMapContainer != null && trackedObject != null && !userMarkers.ContainsKey(trackedObject))
        {
            GameObject uprefab = null;

            switch (markerType)
            {
                // For FoodSpawn, pick active vs inactive prefab
                case MarkerEventManager.MarkerType.FoodSpawn:
                    bool isActive = spawnManager.ActiveFoodSpawnPoints
                                    .Contains(trackedObject.transform);
                    uprefab = isActive
                            ? activeFoodSpawnPointMarkerPrefab
                            : foodSpawnPointMarkerPrefab;
                    break;

                // Everything else uses the standard mapping
                default:
                    uprefab = GetPrefabForMarker(markerType);
                    break;
            }

            if (uprefab != null)
            {
                var umarker = Instantiate(uprefab, userMapContainer);
                umarker.name = trackedObject.name + "_UserMarker";
                umarker.transform.SetAsLastSibling();
                userMarkers[trackedObject] = umarker;
            }
        }

        // ── AGENT MAPS ──
        foreach (var kv in agentMapContainers)
        {
            var info      = kv.Key;       // the AgentMapInfo for this map
            var container = kv.Value;     // its RectTransform
            GameObject prefab = null;
            bool shouldShow = false;

            switch (markerType)
            {
                // Always show your own agent and your allies
                case MarkerEventManager.MarkerType.Agent:
                    prefab     = (trackedObject == info.gameObject)
                                ? agentMarkerPrefab 
                                : allyMarkerPrefab;
                    shouldShow = true;
                    break;

                // Always show enemies on every map
                case MarkerEventManager.MarkerType.Enemy:
                    prefab     = enemyMarkerPrefab;
                    shouldShow = true;
                    break;

                // FOOD: only if this agent discovered that piece of food
                case MarkerEventManager.MarkerType.Food:
                    if (info.knownMarkers.Exists(md =>
                            md.discoveredObject == trackedObject &&
                            md.markerType     == MarkerEventManager.MarkerType.Food))
                    {
                        prefab     = foodMarkerPrefab;
                        shouldShow = true;
                    }
                    break;

                // FOOD SPAWN: only if discovered, then pick active vs inactive prefab
                case MarkerEventManager.MarkerType.FoodSpawn:
                    if (info.knownMarkers.Exists(md =>
                            md.discoveredObject == trackedObject &&
                            md.markerType     == MarkerEventManager.MarkerType.FoodSpawn))
                    {
                        bool isActive = spawnManager.ActiveFoodSpawnPoints.Contains(trackedObject.transform);
                        prefab     = isActive
                                    ? activeFoodSpawnPointMarkerPrefab
                                    : foodSpawnPointMarkerPrefab;
                        shouldShow = true;
                    }
                    break;

                // ENEMY SPAWN: only if discovered
                case MarkerEventManager.MarkerType.EnemySpawn:
                    if (info.knownMarkers.Exists(md =>
                            md.discoveredObject == trackedObject &&
                            md.markerType     == MarkerEventManager.MarkerType.EnemySpawn))
                    {
                        prefab     = enemySpawnPointMarkerPrefab;
                        shouldShow = true;
                    }
                    break;
            }

            if (!shouldShow || prefab == null)
                continue;

            // instantiate on *this* agent’s map only
            var marker = Instantiate(prefab, container);
            marker.name = $"{trackedObject.name}_{markerType}_for_{info.gameObject.name}";
            marker.transform.SetAsLastSibling();    // bring to front

            // track it so we can remove later
            if (!agentMarkers.TryGetValue(trackedObject, out var list))
            {
                list = new List<GameObject>();
                agentMarkers[trackedObject] = list;
            }
            list.Add(marker);
        }
    }


    private void OnMarkerRemovedHandler(GameObject trackedObject)
    {
        if (trackedObject == null) return;

        if (userMarkers.TryGetValue(trackedObject, out var um))
        {
            Destroy(um);
            userMarkers.Remove(trackedObject);
        }

        if (agentMarkers.TryGetValue(trackedObject, out var list))
        {
            foreach (var m in list) Destroy(m);
            agentMarkers.Remove(trackedObject);
        }
    }

    private void Update()
    {
        // Update user‐map markers
        foreach (var kv in new List<GameObject>(userMarkers.Keys))
        {
            var tracked = kv;
            var marker  = userMarkers[tracked];
            if (tracked == null)
            {
                Destroy(marker);
                userMarkers.Remove(tracked);
                continue;
            }

            Vector3 wp = mapCamera.WorldToViewportPoint(tracked.transform.position);
            marker.SetActive(wp.z >= 0);
            var localPos = new Vector2(
                (wp.x - 0.5f) * userMapResolution.x,
                (wp.y - 0.5f) * userMapResolution.y
            );
            marker.GetComponent<RectTransform>().anchoredPosition = localPos;

            var txt = marker.transform.Find("CoordinateText")?.GetComponent<TMP_Text>();
            if (txt != null)
                txt.text = $"({tracked.transform.position.x:F2}, {tracked.transform.position.y:F2}, {tracked.transform.position.z:F2})";
        }

        // Update agent‐map markers
        foreach (var kv in agentMarkers)
        {
            var tracked = kv.Key;
            var list    = kv.Value;
            if (tracked == null)
            {
                list.ForEach(Destroy);
                continue;
            }

            Vector3 wp = mapCamera.WorldToViewportPoint(tracked.transform.position);
            bool vis   = wp.z >= 0;
            var res    = agentMapResolution;

            foreach (var marker in list)
            {
                marker.SetActive(vis);
                var localPos = new Vector2(
                    (wp.x - 0.5f) * res.x,
                    (wp.y - 0.5f) * res.y
                );
                marker.GetComponent<RectTransform>().anchoredPosition = localPos;

                var txt = marker.transform.Find("CoordinateText")?.GetComponent<TMP_Text>();
                if (txt != null)
                    txt.text = $"({tracked.transform.position.x:F2}, {tracked.transform.position.y:F2}, {tracked.transform.position.z:F2})";
            }
        }
    }

    private GameObject GetPrefabForMarker(MarkerEventManager.MarkerType markerType)
    {
        switch (markerType)
        {
            // on the USER map, ALL agents are “allies” (triangles)
            case MarkerEventManager.MarkerType.Agent:
                return allyMarkerPrefab;

            // enemies use the enemy icon
            case MarkerEventManager.MarkerType.Enemy:
                return enemyMarkerPrefab;

            // food & food‐spawn as before
            case MarkerEventManager.MarkerType.Food:
                return foodMarkerPrefab;

            case MarkerEventManager.MarkerType.FoodSpawn:
                return foodSpawnPointMarkerPrefab;

            // ← NEW: enemy‐spawn now shows up too
            case MarkerEventManager.MarkerType.EnemySpawn:
                return enemySpawnPointMarkerPrefab;

            default:
                return null;
>>>>>>> main
        }
    }
}
