using UnityEngine;

public class HabitatDoor : MonoBehaviour
{
    private Habitat parentHabitat;

    void Awake()
    {
        // Get the Habitat component from a parent object.
        parentHabitat = GetComponentInParent<Habitat>();
        if (parentHabitat == null)
        {
            Debug.LogError("HabitatDoor: No parent Habitat found.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only process agents.
        if (other.CompareTag("agent"))
        {
            Debug.Log("HabitatDoor triggered by " + other.name);
            parentHabitat.HandleAgentEntry(other);
        }
    }
}