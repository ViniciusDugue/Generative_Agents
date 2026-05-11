using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentMapInfo : MonoBehaviour
{
    public float detectionRange = 1000f; // Agent's personal sensor range.
    public List<MarkerData> knownMarkers = new List<MarkerData>();

    private void OnEnable()
    {
        // Subscribe to global marker spawn events.
        MarkerEventManager.OnMarkerSpawned += OnGlobalMarkerSpawned;
        MarkerEventManager.OnMarkerRemoved += OnGlobalMarkerRemoved;
    }

    private void OnDisable()
    {
        // Unsubscribe from the global marker spawn events.
        MarkerEventManager.OnMarkerSpawned -= OnGlobalMarkerSpawned;
        MarkerEventManager.OnMarkerRemoved -= OnGlobalMarkerRemoved;
    }

    // Called whenever a marker is spawned in the global event.
    private void OnGlobalMarkerSpawned(GameObject obj, MarkerEventManager.MarkerType markerType)
    {
        // If within detection range, register this object as discovered.
        if (Vector3.Distance(transform.position, obj.transform.position) <= detectionRange)
        {
            MarkerData newMarker = new MarkerData(obj, markerType);
            knownMarkers.Add(newMarker);
            Debug.Log($"{gameObject.name} discovered {markerType} on {obj.name}");
        }
    }

    /// <summary>
    /// Whenever any global marker is removed (food, spawn‑point, etc),
    /// drop it from our knownMarkers so agents no longer “remember” it.
    /// </summary>
    private void OnGlobalMarkerRemoved(GameObject obj)
    {
        knownMarkers.RemoveAll(md => md.discoveredObject == obj);
    }

    // Optionally, provide a method to share map data.
    public List<MarkerData> GetKnownMarkers()
    {
        return knownMarkers;
    }

    // Structure to store marker data for each discovered marker.
    public struct MarkerData
    {
        public MarkerEventManager.MarkerType markerType;

        public Vector3 position;

        public GameObject discoveredObject; // Store the object reference

        public MarkerData(GameObject obj, MarkerEventManager.MarkerType type)
        {
            discoveredObject = obj;
            markerType = type;
            position = obj.transform.position;
        }
    }

}
