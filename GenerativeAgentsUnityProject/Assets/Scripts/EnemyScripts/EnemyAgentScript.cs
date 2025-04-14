using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAgentScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Time to wait at each roam position.")]
    public float roamWaitTime = 2f;  
    [Tooltip("Range used for selecting a random roam target.")]
    public float roamRange = 20f;    
    [Tooltip("Maximum distance from the spawn point the enemy is allowed to wander.")]
    public float patrolRadius = 20f; 

    // Private variables
    private Transform targetAgent;      // The agent (player) to chase
    private bool isChasing = false;     // Are we chasing the agent?
    private bool isPaused = false;      // Is the enemy temporarily paused?
    private FieldOfView fieldOfView;    // Reference to the FieldOfView component
    private NavMeshAgent navAgent;      // The NavMeshAgent for navigation
    private Vector3 roamTarget;         // The current roam destination when not chasing
    private Vector3 spawnPosition;      // The enemy’s spawn position (center of patrol)

    void Start()
    {
        // Save the spawn position as the center of patrol.
        spawnPosition = transform.position;
        
        // Get and verify the FieldOfView component.
        fieldOfView = GetComponent<FieldOfView>();
        if (fieldOfView == null)
        {
            Debug.LogError("EnemyAgentScript: FieldOfView component is missing!");
            enabled = false;
            return;
        }
        
        // Get and verify the NavMeshAgent component.
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogError("EnemyAgentScript: NavMeshAgent component is missing!");
            enabled = false;
            return;
        }
        
        // Start the roaming behavior.
        StartCoroutine(Roam());
    }

    void Update()
    {
        if (isPaused)
            return;

        // If the FieldOfView shows an agent.
        if (fieldOfView.canSeeAgent)
        {
            targetAgent = (fieldOfView.agent != null) ? fieldOfView.agent.transform : null;
            if (targetAgent != null && targetAgent.CompareTag("agent"))
            {
                float distFromSpawn = Vector3.Distance(targetAgent.position, spawnPosition);
                Debug.Log("Distance from target to spawn: " + distFromSpawn + " (patrolRadius: " + patrolRadius + ")");

                // Chase only if within the allowed patrol area.
                if (distFromSpawn <= patrolRadius)
                {
                    isChasing = true;
                    navAgent.SetDestination(targetAgent.position);
                    Debug.Log("Chasing agent: " + targetAgent.name);
                }
                else
                {
                    isChasing = false;
                    navAgent.SetDestination(GetReturnDestination());
                    Debug.Log("Agent left patrol radius, returning to patrol area.");
                }
            }
            else
            {
                isChasing = false;
                navAgent.SetDestination(GetReturnDestination());
            }
        }
        else
        {
            // If we lose sight of the agent while chasing, return to our roam target.
            if (isChasing)
            {
                isChasing = false;
                navAgent.SetDestination(GetReturnDestination());
                Debug.Log("Lost sight of agent, resuming patrol.");
            }
        }
    }

    // Returns the destination for the enemy when not chasing.
    private Vector3 GetReturnDestination()
    {
        if (Vector3.Distance(transform.position, spawnPosition) > patrolRadius)
            return spawnPosition;
        else
            return roamTarget;
    }

    // Roaming coroutine: periodically picks a new roam target within roamRange clamped to patrolRadius.
    IEnumerator Roam()
    {
        while (true)
        {
            if (isPaused || isChasing)
            {
                yield return null;
                continue;
            }

            // Generate a random offset within roamRange.
            Vector3 randomOffset = new Vector3(
                Random.Range(-roamRange, roamRange),
                0,
                Random.Range(-roamRange, roamRange)
            );

            // Set a new roam target relative to the spawnPosition.
            roamTarget = spawnPosition + randomOffset;
            // Clamp roamTarget to be within the patrolRadius.
            if (Vector3.Distance(roamTarget, spawnPosition) > patrolRadius)
            {
                roamTarget = spawnPosition + Vector3.ClampMagnitude(roamTarget - spawnPosition, patrolRadius);
            }

            navAgent.SetDestination(roamTarget);
            Debug.Log("Roaming to: " + roamTarget);

            // Wait until close to the roam target or until roamWaitTime expires.
            float elapsedTime = 0f;
            while (Vector3.Distance(transform.position, roamTarget) > 0.5f && elapsedTime < roamWaitTime)
            {
                if (isPaused || isChasing)
                    break;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(roamWaitTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Pause movement on collision with an agent (if desired).
        if (collision.gameObject.CompareTag("agent"))
        {
            if (!isPaused)
            {
                StartCoroutine(PauseMovement(5f));
            }
        }
    }

    IEnumerator PauseMovement(float pauseDuration)
    {
        isPaused = true;
        float originalSpeed = navAgent.speed;
        navAgent.speed = 0;

        Renderer rend = GetComponent<Renderer>();
        Color originalColor = (rend != null) ? rend.material.color : Color.white;
        if (rend != null)
        {
            rend.material.color = Color.gray;
        }

        yield return new WaitForSeconds(pauseDuration);

        if (rend != null)
        {
            rend.material.color = originalColor;
        }
        navAgent.speed = originalSpeed;
        isPaused = false;
    }

    // Draw a wire sphere to visualize the patrol radius.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        // If the game is running, use spawnPosition; otherwise, use the current position.
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}
