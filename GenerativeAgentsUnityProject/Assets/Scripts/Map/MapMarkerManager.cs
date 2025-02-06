using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapMarkerManager : MonoBehaviour
{
    public Camera mapCamera;                   // Minimap camera
    public RectTransform mapContainer;          // UI container for the map
    public GameObject markerPrefab;             // Marker prefab for tracked objects
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
        // Example: Find all objects by tag. Modify or add more tags as needed.
        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("enemyAgent");
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("food");

        // Add markers for each type of object
        AddMarkers(agents, Color.blue);
        AddMarkers(enemies, Color.magenta);
        AddMarkers(obstacles, Color.green);
    }

    void AddMarkers(GameObject[] objects, Color markerColor)
    {
        foreach (GameObject obj in objects)
        {
            GameObject marker = Instantiate(markerPrefab, mapContainer);
            marker.name = obj.name + "_Marker";

            // Set marker color
            marker.GetComponent<Image>().color = markerColor;

            // Store the marker in the dictionary for tracking
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
                Destroy(marker);  // Clean up if the object is destroyed
                continue;
            }

            // Convert world position to viewport position
            Vector3 worldPos = trackedObject.transform.position;
            Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);

            // Convert viewport position to UI coordinates
            Vector2 mapPos = new Vector2(
                viewportPos.x * mapContainer.rect.width,
                viewportPos.y * mapContainer.rect.height
            );

            // Update the marker's position on the minimap
            marker.GetComponent<RectTransform>().anchoredPosition = mapPos;
        }
    }
}
