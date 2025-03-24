using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// This behavior makes the agent flee from any hostile NPCs (predators) within a specified radius.
/// It uses smoothing to prevent abrupt changes in direction and updates the agent's rotation so that
/// the agent faces away from the predator while fleeing, and once no predator is detected, the agent
/// rotates to look back at the last predator.
/// </summary>
public class FleeBehaviorAgent : AgentBehavior
{
    [Header("Flee Settings")]
    [Tooltip("The radius within which hostile NPCs will trigger the flee behavior.")]
    public float fleeRadius = 10f;
    [Tooltip("The speed at which the agent flees.")]
    public float fleeSpeed = 5f;
    [Tooltip("The tag used to identify hostile NPCs.")]
    public string predatorTag = "enemyAgent";
    
    [Tooltip("Smoothing factor for flee direction adjustments (0 = no change, 1 = instant change).")]
    [Range(0f, 1f)]
    public float smoothFactor = 0.1f;

    [Tooltip("Speed at which the agent rotates toward its target direction.")]
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    // Store the current flee direction for smoothing
    private Vector3 currentFleeDirection = Vector3.zero;
    // Store the closest predator (or the last one seen)
    private Transform lastPredator;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("FleeBehaviorAgent requires a Rigidbody on the same GameObject.");
        }
    }

    // Instead of using OnActionReceived, we use Update to continuously check for predators.
    private void Update()
    {
        FleeFromPredators();
    }

    private void FleeFromPredators()
    {
        if (rb == null) return;

        // Look for predators within the fleeRadius.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, fleeRadius);
        Vector3 targetFleeDirection = Vector3.zero;
        int predatorsFound = 0;
        Transform closestPredator = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag(predatorTag))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPredator = col.transform;
                }

                Vector3 away = transform.position - col.transform.position;
                if (away.magnitude > 0)
                {
                    targetFleeDirection += away.normalized;
                    predatorsFound++;
                }
            }
        }

        if (predatorsFound > 0)
        {
            // Calculate the average flee direction.
            targetFleeDirection /= predatorsFound;
            targetFleeDirection.Normalize();

            // Smoothly interpolate current flee direction toward the target direction.
            currentFleeDirection = Vector3.Lerp(currentFleeDirection, targetFleeDirection, smoothFactor);

            // Set the velocity based on the flee direction.
            rb.velocity = currentFleeDirection * fleeSpeed;

            // Rotate to face the flee direction.
            Quaternion targetRotation = Quaternion.LookRotation(currentFleeDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // Update the last predator reference.
            lastPredator = closestPredator;
        }
        else
        {
            // No predators detected: smoothly decelerate.
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, smoothFactor);
            currentFleeDirection = Vector3.zero;

            // If we have a last predator reference, rotate to face it.
            if (lastPredator != null)
            {
                Vector3 directionToEnemy = lastPredator.position - transform.position;
                if (directionToEnemy != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
    }

    // Optional: visualize the flee radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}
