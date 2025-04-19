using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAgentScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float roamWaitTime = 2f;  // Time to wait at each roam position
    public float roamRange = 20f;    // Range for random roaming

    private Transform targetAgent;   // Reference to the target agent (player)
    private bool isChasing = false;  // Whether the enemy is chasing
    private bool isPaused = false;   // Whether the enemy is paused

    private FieldOfView fieldOfView; // Field of view component reference
    private NavMeshAgent navAgent;   // NavMeshAgent component
    private Vector3 roamTarget;      // Current roam destination

    void Start()
    {
        fieldOfView = GetComponent<FieldOfView>();
        if (fieldOfView == null)
        {
            enabled = false;
            return;
        }

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogError("NavMeshAgent component is missing!");
            enabled = false;
            return;
        }

        // Start the roaming behavior
        StartCoroutine(Roam());
    }

    void Update()
    {
        if (isPaused)
            return;

        // Check if the enemy can see the agent
        if (fieldOfView.canSeeAgent)
        {
            targetAgent = fieldOfView.agent != null ? fieldOfView.agent.transform : null;
            if (targetAgent != null && targetAgent.CompareTag("agent"))
            {
                isChasing = true;
                navAgent.SetDestination(targetAgent.position);
            }
            else
            {
                targetAgent = null;
                isChasing = false;
                navAgent.SetDestination(roamTarget);
            }
        }
        else
        {
            // Resume roaming if the agent is not visible
            if (isChasing)
            {
                isChasing = false;
                navAgent.SetDestination(roamTarget);
            }
        }
    }

    IEnumerator Roam()
    {
        while (true)
        {
            if (isPaused || isChasing)
            {
                yield return null;
                continue;
            }

            // Generate a new roam target relative to the current position
            roamTarget = transform.position + new Vector3(
                Random.Range(-roamRange, roamRange),
                0,
                Random.Range(-roamRange, roamRange)
            );

            // Set the destination for the NavMeshAgent
            navAgent.SetDestination(roamTarget);

            // Wait until the enemy reaches the destination or the wait time elapses
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
        // Pause movement on collision with an agent (if desired)
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
        // Optionally, disable the NavMeshAgent temporarily
        float originalSpeed = navAgent.speed;
        navAgent.speed = 0;

        // Optionally change color to indicate pause
        Renderer rend = GetComponent<Renderer>();
        Color originalColor = rend != null ? rend.material.color : Color.white;
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
}
