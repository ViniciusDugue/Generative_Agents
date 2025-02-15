using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;
    public RectTransform mapContainer;

    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

    private Dictionary<GameObject, GameObject> markers = new Dictionary<GameObject, GameObject>();

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
                (viewportPos.x - 0.5f) * mapContainer.rect.width,
                (viewportPos.y - 0.5f) * mapContainer.rect.height
            );
            marker.GetComponent<RectTransform>().anchoredPosition = localPos;

            // Restore coordinates under each marker
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
            markers.Remove(obj);
        }
    }
}
