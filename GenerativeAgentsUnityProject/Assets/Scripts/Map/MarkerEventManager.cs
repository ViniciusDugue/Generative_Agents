using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MarkerEventManager : MonoBehaviour
{
    // Enum to differentiate between various marker types.
    public enum MarkerType { Agent, Enemy, Food, FoodSpawn, EnemySpawn }

    // Event triggered when an object should have a marker spawned.
    public static event Action<GameObject, MarkerType> OnMarkerSpawned;

    // Call this method when a new object is spawned.
    public static void MarkerSpawned(GameObject obj, MarkerType type)
    {
        OnMarkerSpawned?.Invoke(obj, type);
    }

    // Event when an object’s marker should be removed.
    public static event Action<GameObject> OnMarkerRemoved;

    // Call this method when an object is despawned.
    public static void MarkerRemoved(GameObject obj)
    {
        OnMarkerRemoved?.Invoke(obj);
    }
}
