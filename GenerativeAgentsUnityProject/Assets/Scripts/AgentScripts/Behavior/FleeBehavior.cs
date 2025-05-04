using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// This behavior makes the agent flee from enemies detected by the BehaviorManager,
/// ensuring that the destination remains within the valid NavMesh (i.e., within the arena).
/// When an enemy is in view, the agent calculates a flee destination directly opposite the enemy,
/// samples the NavMesh to get a valid position, and sets that as its destination.
/// If the enemy is no longer in view but was seen recently, the agent rotates to face the enemy’s last known location.
/// A gizmo is drawn in the Scene view to show the computed flee destination.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BehaviorManager))]
public class FleeBehavior : AgentBehavior
{
    [Header("Flee Settings")]
    [Tooltip("How far to flee from the enemy.")]
    public float fleeDistance = 10f;

    [Tooltip("Rotation speed to look at the last known enemy location.")]
    public float rotationSpeed = 5f;

    // Maximum distance to sample for a valid NavMesh position
    public float sampleDistance = 2f;

    private NavMeshAgent navMeshAgent;
    private BehaviorManager behaviorManager;
    private Transform lastEnemy;
    private Vector3 fleeDestination;

    void  OnEnable()
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }
    }
    
    
    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        behaviorManager = GetComponent<BehaviorManager>();

        if (navMeshAgent == null)
            Debug.LogError("FleeBehavior requires a NavMeshAgent component.");
        if (behaviorManager == null)
            Debug.LogError("FleeBehavior requires a BehaviorManager component.");
    }

    private void Update()
    {
        // Check the detection status from BehaviorManager.
        if (behaviorManager.enemyCurrentlyDetected && behaviorManager.enemyTransform != null)
        {
            // Update the last known enemy.
            lastEnemy = behaviorManager.enemyTransform;
            // Calculate the flee direction (directly away from the enemy).
            Vector3 fleeDirection = (transform.position - lastEnemy.position).normalized;
            Vector3 potentialDestination = transform.position + fleeDirection * fleeDistance;

            // Ensure the flee destination is within the nav mesh area.
            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialDestination, out hit, sampleDistance, NavMesh.AllAreas))
            {
                fleeDestination = hit.position;
            }
            else
            {
                // If no valid position is found nearby, fall back to the potential destination.
                fleeDestination = potentialDestination;
            }
            navMeshAgent.SetDestination(fleeDestination);
        }
        else if (behaviorManager.enemyPreviousDetected && lastEnemy != null)
        {
            // Enemy is not in view now, but was seen recently.
            // Rotate the agent to face the last known enemy location.
            Vector3 directionToEnemy = (lastEnemy.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Visualize the flee destination in the Scene view.
    private void OnDrawGizmos()
    {
        if (navMeshAgent != null && navMeshAgent.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, fleeDestination);
            Gizmos.DrawSphere(fleeDestination, 0.5f);
        }
    }
}
