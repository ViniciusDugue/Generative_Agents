using System.Collections;
using UnityEngine;

public class EnemyAgentScript : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 10f; // Range to detect the agent

    [Header("Movement Settings")]
    public float moveSpeed = 2f; // Consistent movement speed for both roaming and chasing
    public float roamRange = 20f; // Range for random roaming around the current position
    public float roamWaitTime = 2f; // Time to wait at each roam position

    private Transform targetAgent; // Current target agent
    private Vector3 roamTarget; // Target position for roaming
    private bool isChasing = false; // Whether the enemy is currently chasing
    private Coroutine roamCoroutine; // Reference to the roaming coroutine

    void Start()
    {
        roamCoroutine = StartCoroutine(Roam()); // Start roaming behavior
    }

    void Update()
    {
        // Find the nearest "agent" in range
        targetAgent = FindNearestAgent();

        if (targetAgent != null)
        {
            if (!isChasing)
            {
                // Stop roaming and start chasing the target agent
                isChasing = true;
                if (roamCoroutine != null)
                {
                    StopCoroutine(roamCoroutine);
                    roamCoroutine = null;
                }
            }
            // Chase the target agent
            ChaseAgent();
        }
        else
        {
            if (isChasing)
            {
                // If no agent is in range, resume roaming
                isChasing = false;
                if (roamCoroutine == null)
                {
                    roamCoroutine = StartCoroutine(Roam());
                }
            }
        }
    }

    Transform FindNearestAgent()
    {
        Transform nearestAgent = null;
        float closestDistance = detectionRange;

        // Find all objects tagged as "agent"
        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");

        foreach (GameObject agent in agents)
        {
            float distance = Vector3.Distance(transform.position, agent.transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                nearestAgent = agent.transform;
            }
        }

        return nearestAgent;
    }

    void ChaseAgent()
    {
        if (targetAgent == null) return;

        // Move towards the target agent at constant speed
        Vector3 direction = (targetAgent.position - transform.position).normalized;
        Vector3 targetPosition = transform.position + direction * moveSpeed * Time.deltaTime;
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
            // Generate a new random roam target relative to the current position
            roamTarget = transform.position + new Vector3(
                Random.Range(-roamRange, roamRange),
                0, // Keep the same Y position to prevent floating or sinking
                Random.Range(-roamRange, roamRange)
            );

            // Move towards the roam target at constant speed
            while (Vector3.Distance(transform.position, roamTarget) > 0.5f)
            {
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
}
