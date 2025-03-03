using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;
    public RectTransform[] mapContainers;  // Support multiple containers

    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

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
        Debug.Log("Initializing markers...");

        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("enemyAgent");
        GameObject[] foods = GameObject.FindGameObjectsWithTag("food");

        Debug.Log($"Agents found: {agents.Length}");
        Debug.Log($"Enemies found: {enemies.Length}");
        Debug.Log($"Food found: {foods.Length}");

        if (agents.Length == 0) Debug.LogWarning("⚠ No objects found with tag 'agent'!");
        if (enemies.Length == 0) Debug.LogWarning("⚠ No objects found with tag 'enemyAgent'!");
        if (foods.Length == 0) Debug.LogWarning("⚠ No objects found with tag 'food'!");

        AddMarkers(agents, agentMarkerPrefab);
        AddMarkers(enemies, enemyMarkerPrefab);
        AddMarkers(foods, foodMarkerPrefab);
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
        List<GameObject> markerList = new List<GameObject>();

        foreach (var container in mapContainers)  // Create a marker in each container
        {
            GameObject marker = Instantiate(prefab, container);
            marker.name = obj.name + "_Marker_" + container.name;
            markerList.Add(marker);
        }

        markers[obj] = markerList;
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
            foreach (var marker in markers[obj])
            {
                Destroy(marker);
            }
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
                foreach (var marker in markerList)
                {
                    Destroy(marker);
                }
                continue;
            }

            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);
            bool isVisible = viewportPos.z >= 0;

            for (int i = 0; i < mapContainers.Length; i++)
            {
                RectTransform container = mapContainers[i];
                GameObject marker = markerList[i];

                marker.SetActive(isVisible);

                Vector2 localPos = new Vector2(
                    (viewportPos.x - 0.5f) * container.rect.width,
                    (viewportPos.y - 0.5f) * container.rect.height
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
        }

        foreach (GameObject obj in toRemove)
        {
            markers.Remove(obj);
        }
    }
}
