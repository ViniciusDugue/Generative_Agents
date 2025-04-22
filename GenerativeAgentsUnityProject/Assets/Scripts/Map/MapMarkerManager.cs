using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(100)]
public class MapMarkerManager : MonoBehaviour
{
    [Header("Map & Camera Prefabs")]
    public GameObject mapPrefab;
    public Camera    agentCameraPrefab;

    [Header("UI Containers")]
    public RectTransform agentMapsContainer;

    [Header("Grid Layout Settings")]
    public int      mapsPerRow   = 5;
    public Vector2 agentMapSize = new Vector2(200, 200);
    public Vector2 gridSpacing  = new Vector2(20, 20);

    [Header("User Map")]
    public Camera   mapCamera;
    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;
    public GameObject foodSpawnPointMarkerPrefab;
    public Vector2 mapResolution = new Vector2(700, 700);

    private RectTransform userMapContainer;
    private Dictionary<GameObject, GameObject> userMarkers = new();

    private AgentMapInfo[] agentInfos;
    private Dictionary<AgentMapInfo, RectTransform>               agentMapContainers   = new();
    private Dictionary<AgentMapInfo, Dictionary<GameObject,GameObject>> agentMarkersByAgent = new();
    private Dictionary<AgentMapInfo, Camera>                      agentCameras         = new();

    void Awake()
    {
        userMapContainer = GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(rt => rt.CompareTag("usermap"));
        if (userMapContainer == null)
            Debug.LogWarning("[MapMarkerManager] No child tagged 'usermap' found.");

        if (agentMapsContainer == null)
        {
            Debug.LogError("[MapMarkerManager] Assign the Agent Maps Container in the Inspector.");
        }
        else
        {
            var grid = agentMapsContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize       = agentMapSize;
                grid.spacing        = gridSpacing;
                grid.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount= mapsPerRow;
            }
            else
            {
                Debug.LogError("[MapMarkerManager] agentMapsContainer needs a GridLayoutGroup component.");
            }
        }
    }

    void Start()
    {
        agentInfos = FindObjectsOfType<AgentMapInfo>();
        CreateAgentMaps();

        MarkerEventManager.OnMarkerSpawned += SafeOnMarkerSpawned;
        MarkerEventManager.OnMarkerRemoved += SafeOnMarkerRemoved;

        foreach (var info in agentInfos)
            SafeOnMarkerSpawned(info.gameObject, MarkerEventManager.MarkerType.Agent);
        foreach (var enemy in GameObject.FindGameObjectsWithTag("enemyAgent"))
            SafeOnMarkerSpawned(enemy, MarkerEventManager.MarkerType.Enemy);
    }

    void OnDisable()
    {
        MarkerEventManager.OnMarkerSpawned -= SafeOnMarkerSpawned;
        MarkerEventManager.OnMarkerRemoved -= SafeOnMarkerRemoved;
    }

    void SafeOnMarkerSpawned(GameObject obj, MarkerEventManager.MarkerType type)
    {
        try { OnMarkerSpawned(obj, type); }
        catch (Exception ex) { Debug.LogError($"[MapMarkerManager] OnMarkerSpawned error: {ex}"); }
    }
    void SafeOnMarkerRemoved(GameObject obj)
    {
        try { OnMarkerRemoved(obj); }
        catch (Exception ex) { Debug.LogError($"[MapMarkerManager] OnMarkerRemoved error: {ex}"); }
    }

    void CreateAgentMaps()
    {
        if (mapPrefab == null || agentCameraPrefab == null)
        {
            Debug.LogError("[MapMarkerManager] Assign mapPrefab & agentCameraPrefab in Inspector.");
            return;
        }

        // Clear out old maps
        foreach (Transform child in agentMapsContainer)
            Destroy(child.gameObject);

        // For each agent, spin up one UI map + one off‑screen camera
        for (int i = 0; i < agentInfos.Length; i++)
        {
            var info = agentInfos[i];

            // 1) Instantiate the UI map under your GridLayoutGroup container:
            var mapGO = Instantiate(mapPrefab, agentMapsContainer, false);
            mapGO.name = $"AgentMap-{i}";
            var canvasRT = mapGO
            .GetComponentsInChildren<RectTransform>(true)
            .First(rt => rt.CompareTag("agentmap"));

            // 2) Build a transparent RenderTexture for your markers:
            var rt = new RenderTexture(
                (int)agentMapSize.x,
                (int)agentMapSize.y,
                /*depth:*/ 16
            );

            // 3) Instantiate a world‑space camera (no parent!)
            var cam = Instantiate(agentCameraPrefab);
            cam.name = $"AgentCam-{i}";
            cam.targetTexture = rt;

            // → Make sure your agentCameraPrefab is set to Orthographic
            //    and Clear Flags = Solid Color with alpha=0,
            //    Culling Mask only includes your marker layers.
            // → Now position it above the agent and point straight down:
            var agentPos = info.gameObject.transform.position;
            cam.transform.position = agentPos + Vector3.up * 100f; 
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 4) In the UI, leave your static RawImage alone (it shows the 2DMapTexture),
            //    then add a second RawImage for the RT overlay:
            var overlayGO = new GameObject("MarkerOverlay", typeof(RawImage));
            overlayGO.transform.SetParent(canvasRT, false);
            var overlay = overlayGO.GetComponent<RawImage>();
            overlay.texture = rt;
            // stretch it full‑size:
            overlay.rectTransform.anchorMin    = Vector2.zero;
            overlay.rectTransform.anchorMax    = Vector2.one;
            overlay.rectTransform.offsetMin    = Vector2.zero;
            overlay.rectTransform.offsetMax    = Vector2.zero;

            // 5) Cache references for marker logic, etc.
            agentMapContainers[info]  = canvasRT;
            agentMarkersByAgent[info] = new Dictionary<GameObject, GameObject>();
            agentCameras[info]        = cam;
        }
    }

    private void OnMarkerSpawned(GameObject obj, MarkerEventManager.MarkerType type)
    {
        // USER MAP
        if (userMapContainer && obj && !userMarkers.ContainsKey(obj))
        {
            var prefab = GetPrefabForMarker(type);
            if (prefab) userMarkers[obj] = Instantiate(prefab, userMapContainer);
        }

        // AGENT MAPS
        foreach (var info in agentInfos)
        {
            if (!agentMapContainers.ContainsKey(info) ||
                !agentMarkersByAgent.ContainsKey(info))
                continue;

            var dict      = agentMarkersByAgent[info];
            var container = agentMapContainers[info];
            if (!obj || dict.ContainsKey(obj)) continue;

            if ((type == MarkerEventManager.MarkerType.Food ||
                 type == MarkerEventManager.MarkerType.FoodSpawn)
                && !info.knownMarkers.Any(d =>
                       d.discoveredObject == obj &&
                       d.markerType      == type))
                continue;

            var prefab = GetPrefabForMarker(type);
            if (prefab) dict[obj] = Instantiate(prefab, container);
        }
    }

    private void OnMarkerRemoved(GameObject obj)
    {
        if (obj == null) return;

        if (userMarkers.ContainsKey(obj))
        {
            Destroy(userMarkers[obj]);
            userMarkers.Remove(obj);
        }

        foreach (var info in agentInfos)
        {
            if (!agentMarkersByAgent.ContainsKey(info)) continue;
            var dict = agentMarkersByAgent[info];
            if (dict.ContainsKey(obj))
            {
                Destroy(dict[obj]);
                dict.Remove(obj);
            }
        }
    }

    void Update()
    {
        UpdateMarkerPositions(userMarkers, userMapContainer, mapResolution);

        foreach (var info in agentInfos)
        {
            if (!agentMapContainers.ContainsKey(info) ||
                !agentMarkersByAgent.ContainsKey(info))
                continue;

            var container = agentMapContainers[info];
            var dict      = agentMarkersByAgent[info];

            foreach (var data in info.knownMarkers)
            {
                if (!dict.ContainsKey(data.discoveredObject))
                {
                    var prefab = GetPrefabForMarker(data.markerType);
                    if (prefab)
                        dict[data.discoveredObject] = Instantiate(prefab, container);
                }
            }

            UpdateMarkerPositions(dict, container, agentMapSize);
        }
    }

    private void UpdateMarkerPositions(
        Dictionary<GameObject, GameObject> markers,
        RectTransform container,
        Vector2 resolution)
    {
        var toRemove = new List<GameObject>();
        foreach (var kv in markers)
        {
            var tracked = kv.Key;
            var mark    = kv.Value;
            if (!tracked)
            {
                toRemove.Add(tracked);
                Destroy(mark);
                continue;
            }

            Vector3 worldPos = tracked.transform.position;
            Vector3 vp       = mapCamera.WorldToViewportPoint(worldPos);
            mark.SetActive(vp.z >= 0);

            Vector2 local = new Vector2(
                (vp.x - 0.5f) * resolution.x,
                (vp.y - 0.5f) * resolution.y
            );
            if (mark.TryGetComponent<RectTransform>(out var rt))
                rt.anchoredPosition = local;

            var txt = mark.transform.Find("CoordinateText");
            if (txt && txt.TryGetComponent<TMP_Text>(out var tmp))
                tmp.text = $"({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})";
        }
        foreach (var k in toRemove) markers.Remove(k);
    }

    // **Replaced the switch-expression** with a classic switch for full compatibility
    private GameObject GetPrefabForMarker(MarkerEventManager.MarkerType type)
    {
        switch (type)
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
}
