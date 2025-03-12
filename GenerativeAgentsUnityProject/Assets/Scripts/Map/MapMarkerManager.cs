using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
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
        }
    }
}
