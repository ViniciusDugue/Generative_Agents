using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// This behavior makes the agent flee from any hostile NPCs (predators) within a specified radius.
/// It uses smoothing to prevent abrupt changes in direction.
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

    private Rigidbody rb;
    // Store the current flee direction for smoothing
    private Vector3 currentFleeDirection = Vector3.zero;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("FleeBehaviorAgent requires a Rigidbody on the same GameObject.");
        }
    }

    // Instead of OnActionReceived, we'll use Update to continuously check for predators.
    private void Update()
    {
        FleeFromPredators();
    }

    private void FleeFromPredators()
    {
        if (rb == null) return;

        // Check for predators within fleeRadius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, fleeRadius);
        Vector3 targetFleeDirection = Vector3.zero;
        int predatorsFound = 0;

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag(predatorTag))
            {
                // Calculate direction away from the predator.
                Vector3 away = transform.position - col.transform.position;
                float distance = away.magnitude;
                if (distance > 0)
                {
                    // Closer predators have more influence.
                    targetFleeDirection += away.normalized * (fleeRadius - distance);
                    predatorsFound++;
                }
            }
        }

        if (predatorsFound > 0)
        {
            // Average and normalize the target flee direction.
            targetFleeDirection /= predatorsFound;
            targetFleeDirection.Normalize();

            // Smoothly interpolate toward the target flee direction.
            currentFleeDirection = Vector3.Lerp(currentFleeDirection, targetFleeDirection, smoothFactor);

            // Apply the flee velocity.
            rb.velocity = currentFleeDirection * fleeSpeed;
        }
        else
        {
            // No predators detected: smoothly decelerate.
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, smoothFactor);
            currentFleeDirection = Vector3.zero;
        }
    }

    // Optional: visualize the flee radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}
