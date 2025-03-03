using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;
    public RectTransform[] mapContainers; // For example, [0] = UserMap, [1] = AgentMap

    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

    // Define marker scales for each map container.
    // For instance, markerScales[0] applies to mapContainers[0] and so on.
    public float[] markerScales; 
    // Define text scales for the CoordinateText inside each marker prefab.
    public float[] markerTextScales;

    // For multiple maps, we keep a marker for each object per map.
    private Dictionary<GameObject, List<GameObject>> markers = new Dictionary<GameObject, List<GameObject>>();

    void Start()
    {
        InitializeMarkers();
    }

    void Update()
    {
        UpdateMarkerPositions();
    }

    private void InitializeMarkers()
    {
        AddMarkers(GameObject.FindGameObjectsWithTag("agent"), agentMarkerPrefab);
        AddMarkers(GameObject.FindGameObjectsWithTag("enemyAgent"), enemyMarkerPrefab);
        AddMarkers(GameObject.FindGameObjectsWithTag("food"), foodMarkerPrefab);
    }

    private void AddMarkers(GameObject[] objects, GameObject prefab)
    {
        foreach (GameObject obj in objects)
        {
            if (!markers.ContainsKey(obj))
            {
                AddMarker(obj, prefab);
            }
        }
    }

    private void AddMarker(GameObject obj, GameObject prefab)
    {
        // Create one marker per map container.
        List<GameObject> markerList = new List<GameObject>();
        for (int i = 0; i < mapContainers.Length; i++)
        {
            RectTransform container = mapContainers[i];
            GameObject marker = Instantiate(prefab, container);
            marker.name = obj.name + "_Marker_" + container.name;

            // Set marker scale based on markerScales array (if assigned).
            if (markerScales != null && markerScales.Length > i)
            {
                marker.transform.localScale = new Vector3(markerScales[i], markerScales[i], 1f);
            }
            else
            {
                marker.transform.localScale = Vector3.one;
            }

            // Also adjust the scale of the TextMeshPro component within the marker.
            Transform textTransform = marker.transform.Find("CoordinateText");
            if (textTransform != null)
            {
                if (markerTextScales != null && markerTextScales.Length > i)
                {
                    // Scale the text object by the given factor.
                    textTransform.localScale = new Vector3(markerTextScales[i], markerTextScales[i], 1f);
                }
                else
                {
                    // Default scale for text if not assigned.
                    textTransform.localScale = Vector3.one;
                }
            }
            markerList.Add(marker);
        }
        markers[obj] = markerList;
    }

    public void RegisterMarker(GameObject obj)
    {
        if (obj.CompareTag("agent"))
            AddMarker(obj, agentMarkerPrefab);
        else if (obj.CompareTag("enemyAgent"))
            AddMarker(obj, enemyMarkerPrefab);
        else if (obj.CompareTag("food"))
            AddMarker(obj, foodMarkerPrefab);
    }

    public void RemoveMarker(GameObject obj)
    {
        if (markers.ContainsKey(obj))
        {
            foreach (GameObject marker in markers[obj])
                Destroy(marker);
            markers.Remove(obj);
        }
    }

    private void UpdateMarkerPositions()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var pair in markers)
        {
            GameObject trackedObject = pair.Key;
            List<GameObject> markerList = pair.Value;

            if (trackedObject == null)
            {
                toRemove.Add(trackedObject);
                foreach (GameObject marker in markerList)
                    Destroy(marker);
                continue;
            }

            Vector3 worldPos = trackedObject.transform.position;
            // Get viewport coordinates (0 to 1) using the map camera.
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);
            bool isVisible = viewportPos.z >= 0;

            // Update markers for each map container using the same math as before.
            for (int i = 0; i < mapContainers.Length; i++)
            {
                RectTransform container = mapContainers[i];
                GameObject marker = markerList[i];

                marker.SetActive(isVisible);
                if (!isVisible)
                    continue;

                // Use the working math from your single‑map solution.
                Vector2 localPos = new Vector2(
                    (viewportPos.x - 0.5f) * container.rect.width,
                    (viewportPos.y - 0.5f) * container.rect.height
                );
                marker.GetComponent<RectTransform>().anchoredPosition = localPos;

                // Optionally update coordinate text if desired.
                Transform textTransform = marker.transform.Find("CoordinateText");
                if (textTransform != null)
                {
                    TMP_Text tmpText = textTransform.GetComponent<TMP_Text>();
                    if (tmpText != null)
                        tmpText.text = $"({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})";
                }
            }
        }

        foreach (GameObject obj in toRemove)
            markers.Remove(obj);
    }
}
