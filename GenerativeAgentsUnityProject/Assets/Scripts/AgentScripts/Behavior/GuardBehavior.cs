using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GuardBehavior : MonoBehaviour
{
    // all guardhabitat states
    public enum GuardState
    {
        GoTowardsHabitat,
        PatrolHabitat,
        ChasePest
    }

    public GuardState currentState = GuardState.GoTowardsHabitat;
    public NavMeshAgent navAgent;

    [Header("Habitat Settings")]
    public Transform habitat;
    public float goTowardsStoppingDistance = 2f;

    [Header("Patrol Settings")]
    public float patrolRadius = 5f;
    public float patrolInterval = 3f;
    private float patrolTimer = 0f;
    private Vector3 patrolDestination;

    [Header("Chase Settings")]
    public float detectionRadius = 10f;
    public float chaseDuration = 5f;
    public float maxChaseDistance = 15f;
    private float chaseTimer = 0f;
    private Transform targetPest;

    [Header("Collision Settings")]
    public float collisionDamage = 20f;

    // initialize starting values
    void Start()
    {
        habitat = Habitat.Instance.gameObject.transform;
        navAgent = GetComponent<NavMeshAgent>(); 
        currentState = GuardState.GoTowardsHabitat;
    }

    void Update()
    {
        // only check for pests if in patroling state
        CheckForPests();

        switch (currentState)
        {
            case GuardState.GoTowardsHabitat:
                GoTowardsHabitat();
                break;

            case GuardState.PatrolHabitat:
                PatrolHabitat();
                break;

            case GuardState.ChasePest:
                ChasePest();
                break;
        }
    }

    // checks for nearest pest
    void CheckForPests()
    {
        if (currentState != GuardState.PatrolHabitat)
            return;

        GameObject[] pests = GameObject.FindGameObjectsWithTag("pest");
        float nearestDistance = Mathf.Infinity;
        Transform nearestPest = null;
        foreach (GameObject pest in pests)
        {
            float d = Vector3.Distance(transform.position, pest.transform.position);
            if (d < detectionRadius && d < nearestDistance)
            {
                nearestDistance = d;
                nearestPest = pest.transform;
            }
        }
        if (nearestPest != null)
        {
            currentState = GuardState.ChasePest;
            targetPest = nearestPest;
            chaseTimer = 0f;
        }
    }

    // go towards habitat and then swap to patrol habitat state
    void GoTowardsHabitat()
    {
        if (habitat == null) return;
        navAgent.SetDestination(habitat.position);
        if (!navAgent.pathPending && navAgent.remainingDistance <= goTowardsStoppingDistance)
        {
            currentState = GuardState.PatrolHabitat;
            patrolTimer = 0f;
            patrolDestination = GetPatrolDestination();
            navAgent.SetDestination(patrolDestination);
        }
    }

    // patrol around habitat
    void PatrolHabitat()
    {
        patrolTimer += Time.deltaTime;
        if ((!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance) || patrolTimer >= patrolInterval)
        {
            patrolDestination = GetPatrolDestination();
            navAgent.SetDestination(patrolDestination);
            patrolTimer = 0f;
        }
    }

    // create random point to patrol habitat
    Vector3 GetPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += habitat.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return habitat.position;
    }

    // chase pest until duration or until you have reached too far from the habitat
    void ChasePest()
    {
        if (targetPest == null)
        {
            currentState = GuardState.GoTowardsHabitat;
            return;
        }

        navAgent.SetDestination(targetPest.position);
        chaseTimer += Time.deltaTime;
        float distanceFromHabitat = Vector3.Distance(transform.position, habitat.position);
        if (chaseTimer >= chaseDuration || distanceFromHabitat >= maxChaseDistance)
        {
            currentState = GuardState.GoTowardsHabitat;
            targetPest = null;
        }
    }
}
