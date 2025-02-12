using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            // Optionally, adjust marker size if needed.
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(80, 80);  // Change to your desired size.

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
                Destroy(marker);  // Clean up if the object is destroyed.
                continue;
            }

            // Convert the world position to viewport coordinates using the minimap camera.
            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);

            // Optional: Hide markers if the object is behind the camera.
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
            // This assumes that your minimap's RectTransform pivot is set to (0.5, 0.5).
            Vector2 localPos = new Vector2(
                (viewportPos.x - 0.5f) * mapContainer.rect.width,
                (viewportPos.y - 0.5f) * mapContainer.rect.height
            );

            // Update the marker's position on the minimap.
            marker.GetComponent<RectTransform>().anchoredPosition = localPos;
        }
    }
}
