using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PestNPC : MonoBehaviour
{
    public enum PestState
    {
        Wandering,            
        FoundHabitat,
        GoToHabitatWithFood,
        EatFood,
        RunAway
    }

    public PestState currentState = PestState.Wandering;
    public NavMeshAgent navAgent;

    [Header("Wandering Settings")]
    public float wanderRadius = 10f;
    public float roamTimer = 5f;      
    public float minRoamDistance = 3f; 
    private float wanderTimerCounter;

    [Header("Habitat Settings")]
    public float habitatDetectionRange = 15f;
    public float habitatStoppingDistance = 2f;

    public Transform currentHabitat;
    public bool habitatHasFood = true;
    public float eatDuration = 3f;
    public int foodPortionsConsumed = 1;

    [Header("RunAway Settings")]
    public float detectionRadius = 5f;
    public float runAwayDistance = 10f;
    public float runawayTimer = 3f;
    private float runawayTimerCounter = 0f;

    private Transform threat;
    public GameObject[] agents;
    public float dist;

    private Coroutine eatFoodRoutine = null;

    private Vector3 runawayDestination;
    private bool runawayDestinationSet = false;

    public float health = 100f;
    public float collisionDamage = 40f;

    void Start()
    {
        currentHabitat = Habitat.Instance.gameObject.transform;
        navAgent = GetComponent<NavMeshAgent>();
        wanderTimerCounter = 0f;
    }

    void Update()
    {
        agents = GameObject.FindGameObjectsWithTag("agent");
        bool agentInRange = false;
        Transform nearestAgent = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject agentObj in agents)
        {
            dist = Vector3.Distance(transform.position, agentObj.transform.position);
            if (dist < detectionRadius)
            {
                agentInRange = true;
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestAgent = agentObj.transform;
                }
            }
        }

        // if agent in range, run away
        if (agentInRange)
        {
            threat = nearestAgent;
            if (currentState == PestState.EatFood && eatFoodRoutine != null)
            {
                StopCoroutine(eatFoodRoutine);
                eatFoodRoutine = null;
            }
            currentState = PestState.RunAway;
        }
        else
        {
            threat = null;
        }

        switch (currentState)
        {
            // npc wanders around and actively checks if habitat in range
            case PestState.Wandering:
                Wander();

                if (DetectHabitat())
                {
                    currentState = PestState.FoundHabitat;
                }
                break;

            // if habitat is found, go to the habitat
            case PestState.FoundHabitat:
                if (currentHabitat != null && habitatHasFood)
                {
                    currentState = PestState.GoToHabitatWithFood;
                }
                else if(Habitat.Instance.storedFood<=0)
                {
                    // If no food is in habitat, keep wandering

                    currentState = PestState.Wandering;
                }
                break;

            // goes towards habitat until it reaches it, then swap to EatFood and starts eating it
            case PestState.GoToHabitatWithFood:
                if (currentHabitat != null)
                {
                    navAgent.SetDestination(currentHabitat.position);
                    if (Vector3.Distance(transform.position, currentHabitat.position) <= habitatStoppingDistance)
                    {
                        currentState = PestState.EatFood;
                        // Start and store the eating coroutine.
                        eatFoodRoutine = StartCoroutine(EatFoodCoroutine());
                    }
                }
                break;

            //continue eating food in habitat until scared by agent
            case PestState.EatFood:
                break;

            // if pest is near agent, swap to this state and run away from agent perpendicular to the direction towards agent
            case PestState.RunAway:
                if (!runawayDestinationSet)
                {
                    SetRunAwayDestination();
                    runawayDestinationSet = true;
                    runawayTimerCounter = 0f;
                }

                runawayTimerCounter += Time.deltaTime;

                // if pest reached its runaway destination or has ran away for a certain amount of time then start wandering again
                if ((!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance) || runawayTimerCounter >= runawayTimer)
                {
                    runawayDestinationSet = false;
                    currentState = PestState.Wandering;
                }
                break;
        }
    }

    // wander function that chooses a new destination if it has reached the wandering time or its wandering destination
    void Wander()
    {
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            Vector3 newDestination = GetValidRoamDestination();
            navAgent.SetDestination(newDestination);
            wanderTimerCounter = 0f; // Reset timer on arrival
        }
        else
        {
            wanderTimerCounter += Time.deltaTime;
            if (wanderTimerCounter >= roamTimer)
            {
                Vector3 newDestination = GetValidRoamDestination();
                navAgent.SetDestination(newDestination);
                wanderTimerCounter = 0f;
            }
        }
    }

    // searches for a point in the nav mesh for wandering
    Vector3 GetValidRoamDestination()
    {
        Vector3 newDestination = transform.position;
        int attempts = 0;
        int maxAttempts = 10;
        while (attempts < maxAttempts && Vector3.Distance(transform.position, newDestination) < minRoamDistance)
        {
            newDestination = RandomNavSphere(transform.position, wanderRadius, -1);
            attempts++;
        }
        return newDestination;
    }


    // gets random point in nav mesh
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randomDirection = Random.insideUnitSphere * dist;
        randomDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    // checks if habitat in range
    bool DetectHabitat()
    {
        if (currentHabitat != null)
        {
            float distanceToHabitat = Vector3.Distance(transform.position, currentHabitat.position);
            if (distanceToHabitat <= habitatDetectionRange)
            {
                return true;
            }
        }
        return false;
    }

    // a looping coroutine for a pest eating food from habitat
        IEnumerator EatFoodCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(eatDuration);
            if (currentHabitat != null)
            {
                // Call RemoveFood on the habitat singleton.
                Habitat.Instance.RemoveFood(foodPortionsConsumed);
                EndSimMetricsUI.Instance.IncrementFoodEatenByPests();
            }
        }
    }

    //calculatese runaway destination
    void SetRunAwayDestination()
    {
        if (threat == null || currentHabitat == null)
            return;

        // calculate middle vector inbetween habitat to pest and pest to agent
        Vector3 habitatToPest = (transform.position - currentHabitat.position).normalized;
        Vector3 pesttoAgent = (threat.position - transform.position).normalized;

        habitatToPest.y = 0;
        pesttoAgent.y = 0;
        Vector3 middleDirection = (habitatToPest + pesttoAgent).normalized;

        //fallback to running away from habitat
        if(middleDirection == Vector3.zero)
        {
            middleDirection = habitatToPest;
        }
        NavMeshHit hit;
        float angleIncrement = 5f;
        int maxAttempts = Mathf.CeilToInt(360f / angleIncrement);
        int attempts = 0;
        bool found = false;
        Vector3 rotatedDirection;
        Vector3 candidate = transform.position;

        // try potential runnaway directions/locations
        while (attempts < maxAttempts && !found)
        {
            rotatedDirection = Quaternion.Euler(0, angleIncrement * attempts, 0) * middleDirection;
            candidate = transform.position + rotatedDirection * runAwayDistance;
            
            if (NavMesh.SamplePosition(candidate, out hit, 1.0f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) >= runAwayDistance)
                {
                    found = true;
                    runawayDestination = hit.position;
                    navAgent.SetDestination(runawayDestination);
                    break;
                }
            }
            attempts++;
        }

        //if no direction found, fall back to last candidate direction/location
        if (!found)
        {
            runawayDestination = candidate;
            navAgent.SetDestination(runawayDestination);
        }
    }

    // runs away from threat 
    void RunAwayFromThreat()
    {
        if (threat == null) return;
        if (!runawayDestinationSet)
        {
            SetRunAwayDestination();
            runawayDestinationSet = true;
            runawayTimerCounter = 0f;
        }
    }

    // if colliding with agent, damage the pest
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("agent"))
        {
            Debug.Log("Collision Detected with an Agent");
            health -= collisionDamage;
            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
