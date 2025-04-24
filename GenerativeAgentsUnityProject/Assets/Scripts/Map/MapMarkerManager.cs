using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    // LEGACY EVENT for MapEncoder or any other subscriber
    public static event Action<Dictionary<string, GameObject>> mapDictFullyBuilt;

    [Header("Camera & Map Prefab")]
    public Camera    mapCamera;
    public GameObject mapPrefab;

    [Header("Marker Prefabs")]
    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;
    public GameObject foodSpawnPointMarkerPrefab;

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
        // — USER MAP —
        if (userMapContainer != null
         && trackedObject != null
         && !userMarkers.ContainsKey(trackedObject))
        {
            GameObject prefab = GetPrefabForMarker(markerType);
            if (prefab != null)
            {
                var m = Instantiate(prefab, userMapContainer);
                m.name = trackedObject.name + "_UserMarker";
                userMarkers[trackedObject] = m;
            }
        }

        // — AGENT MAPS —
        if (trackedObject == null) return;
        GameObject agentPrefab = GetPrefabForMarker(markerType);
        if (agentPrefab == null)    return;

        foreach (var kv in agentMapContainers)
        {
            AgentMapInfo info       = kv.Key;
            RectTransform container = kv.Value;

            // Only Food/FoodSpawn if discovered; otherwise always show Agent/Enemy
            bool discovered = false;
            if (markerType == MarkerEventManager.MarkerType.Food ||
                markerType == MarkerEventManager.MarkerType.FoodSpawn)
            {
                foreach (var md in info.knownMarkers)
                {
                    if (md.discoveredObject == trackedObject && md.markerType == markerType)
                    {
                        discovered = true;
                        break;
                    }
                }
            }
            else discovered = true;

            if (!discovered) continue;

            // Spawn & track
            var agentMarker = Instantiate(agentPrefab, container);
            agentMarker.name = $"{trackedObject.name}_AgentMarker_for_{info.gameObject.name}";

            if (!agentMarkers.TryGetValue(trackedObject, out var list))
            {
                list = new List<GameObject>();
                agentMarkers[trackedObject] = list;
            }
            list.Add(agentMarker);
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
            case MarkerEventManager.MarkerType.Agent:     return agentMarkerPrefab;
            case MarkerEventManager.MarkerType.Enemy:     return enemyMarkerPrefab;
            case MarkerEventManager.MarkerType.Food:      return foodMarkerPrefab;
            case MarkerEventManager.MarkerType.FoodSpawn: return foodSpawnPointMarkerPrefab;
            default:                                      return null;
        }
    }
}
