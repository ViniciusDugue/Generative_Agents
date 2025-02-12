using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;                   // Minimap camera
    public RectTransform mapContainer;         // UI container for the visible minimap area

    // Separate marker prefabs for each object type
    public GameObject agentMarkerPrefab;
    public GameObject enemyMarkerPrefab;
    public GameObject foodMarkerPrefab;

    // Dictionary to track which world object corresponds to which marker UI element.
    private Dictionary<GameObject, GameObject> markers = new Dictionary<GameObject, GameObject>();

    void Start()
    {
        InitializeMarkers();
    }

    void Update()
    {
        UpdateMarkers();
    }

    void InitializeMarkers()
    {
        // Find objects by tag
        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("enemyAgent");
        GameObject[] food = GameObject.FindGameObjectsWithTag("food");

        // Instantiate markers using the appropriate prefab for each type.
        AddMarkers(agents, agentMarkerPrefab);
        AddMarkers(enemies, enemyMarkerPrefab);
        AddMarkers(food, foodMarkerPrefab);
    }

    void AddMarkers(GameObject[] objects, GameObject prefab)
    {
        foreach (GameObject obj in objects)
        {
            // Instantiate the specific marker prefab for this object type as a child of the minimap container.
            GameObject marker = Instantiate(prefab, mapContainer);
            marker.name = obj.name + "_Marker";

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(20, 20);  // Change size.

            // Store the mapping between the tracked object and its marker.
            markers[obj] = marker;
        }
    }

    void UpdateMarkers()
    {
        foreach (var pair in markers)
        {
            GameObject trackedObject = pair.Key;
            GameObject marker = pair.Value;

            if (trackedObject == null)
            {
                Destroy(marker);
                continue;
            }

            // Convert the world position to viewport coordinates using the minimap camera.
            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);

            // Hide the marker if it's behind the camera.
            if (viewportPos.z < 0)
            {
                marker.SetActive(false);
                continue;
            }
            else
            {
                marker.SetActive(true);
            }

            // Adjust the viewport coordinates for the minimap panel's pivot.
            // Assuming the minimap's RectTransform pivot is (0.5, 0.5).
            Vector2 localPos = new Vector2(
                (viewportPos.x - 0.5f) * mapContainer.rect.width,
                (viewportPos.y - 0.5f) * mapContainer.rect.height
            );
            marker.GetComponent<RectTransform>().anchoredPosition = localPos;

            // Update the coordinate text using TextMeshPro.
            Transform textTransform = marker.transform.Find("CoordinateText");
            if (textTransform != null)
            {
                TMP_Text tmpText = textTransform.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    // Format the world coordinates to two decimal places.
                    tmpText.text = "(" +
                        worldPos.x.ToString("F2") + ", " +
                        worldPos.y.ToString("F2") + ", " +
                        worldPos.z.ToString("F2") + ")";
                }
                else
                {
                    Debug.LogWarning("No TMP_Text component found on 'CoordinateText' in " + marker.name);
                }
            }
            else
            {
                Debug.LogWarning("'CoordinateText' child not found in " + marker.name);
            }
        }
    }

}
