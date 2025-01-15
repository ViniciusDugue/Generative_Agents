using System.Collections;
using UnityEngine;

public class EnemyAgentScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f; // Consistent movement speed for both roaming and chasing
    public float roamRange = 20f; // Range for random roaming around the current position
    public float roamWaitTime = 2f; // Time to wait at each roam position

    private Transform targetAgent; // Current target agent
    private Vector3 roamTarget; // Target position for roaming
    private bool isChasing = false; // Whether the enemy is currently chasing

    // Reference to the FieldOfView component
    private FieldOfView fieldOfView;

    // Flags to control enemy behavior
    private bool isPaused = false; // Indicates if the enemy is currently paused

    void Start()
    {
        // Get the FieldOfView component attached to the same GameObject
        fieldOfView = GetComponent<FieldOfView>();
        if (fieldOfView == null)
        {
            enabled = false;
            return;
        }

        // Start the roaming behavior
        StartCoroutine(Roam());
    }

    void Update()
    {
        // If the enemy is paused, do not execute any movement or chasing logic
        if (isPaused)
            return;

        // Use FieldOfView to determine if an agent is visible
        if (fieldOfView.canSeeAgent)
        {
            // Get the target agent from FieldOfView
            targetAgent = fieldOfView.agent != null ? fieldOfView.agent.transform : null;

            if (targetAgent != null && targetAgent.CompareTag("agent"))
            {
                if (!isChasing)
                {
                    // Start chasing the target agent
                    isChasing = true;
                }
                // Chase the target agent
                ChaseAgent();
            }
            else
            {
                // If the detected object is not tagged as "agent", ignore it
                targetAgent = null;
                if (isChasing)
                {
                    // Resume roaming if currently chasing
                    isChasing = false;
                }
            }
        }
        else
        {
            if (isChasing)
            {
                // If no agent is in view, resume roaming
                isChasing = false;
            }
        }
    }

    void ChaseAgent()
    {
        if (targetAgent == null) return;

        // Move towards the target agent at constant speed
        Vector3 direction = (targetAgent.position - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, targetAgent.position, moveSpeed * Time.deltaTime);

        // Smoothly rotate towards the target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    IEnumerator Roam()
    {
        while (true)
        {
            // If the enemy is paused or chasing, skip roaming
            if (isPaused || isChasing)
            {
                yield return null;
                continue;
            }

            // Generate a new random roam target relative to the current position
            roamTarget = transform.position + new Vector3(
                Random.Range(-roamRange, roamRange),
                0, // Keep the same Y position to prevent floating or sinking
                Random.Range(-roamRange, roamRange)
            );

            // Move towards the roam target at constant speed
            while (Vector3.Distance(transform.position, roamTarget) > 0.5f)
            {
                // If the enemy is paused or starts chasing, exit the roaming loop
                if (isPaused || isChasing)
                {
                    break;
                }

                Vector3 direction = (roamTarget - transform.position).normalized;

                // Move towards the roam target smoothly
                transform.position = Vector3.MoveTowards(transform.position, roamTarget, moveSpeed * Time.deltaTime);

                // Smoothly rotate towards the roam target
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                yield return null; // Wait until the next frame
            }

            // Wait briefly before choosing a new roam target
            yield return new WaitForSeconds(roamWaitTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the enemy collided with a Food Gatherer Agent
        if (collision.gameObject.CompareTag("agent"))
        {
            if (!isPaused)
            {
                StartCoroutine(PauseMovement(5f));
            }
        }
    }

    /// <summary>
    /// Pauses the enemy's movement and behavior for a specified duration.
    /// </summary>
    /// <param name="pauseDuration">Duration in seconds to pause movement.</param>
    IEnumerator PauseMovement(float pauseDuration)
    {
        isPaused = true;

        // Optionally, change color to indicate pause
        Renderer rend = GetComponent<Renderer>();
        Color originalColor = Color.white;
        if (rend != null)
        {
            originalColor = rend.material.color;
            rend.material.color = Color.gray;
        }

        // Wait for the pause duration
        yield return new WaitForSeconds(pauseDuration);

        // Revert color back to original
        if (rend != null)
        {
            rend.material.color = originalColor;
        }

        isPaused = false;
    }
}
