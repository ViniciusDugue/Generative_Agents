using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BuildWallBehavior : AgentBehavior
{
    [Header("Required GameObjects")]
    public GameObject smallWall;
    public GameObject destinationPrefab;

    [Header("Movement Settings")]
    public NavMeshAgent navAgent;
    public float destinationStopDistance = 1f;

    [Header("Control Booleans")]
    public bool startBehavior = false;
    public bool interrupt = false;
    public bool isHoldingWall = false;

    public enum AgentState
    {
        Idle,
        MoveToHabitat,
        WaitAtHabitat,
        MoveToBuildPosition,
        WaitAtBuildPosition,
        CreateWall
    }
    public AgentState currentState = AgentState.Idle;

    private bool waitingAtHabitat = false;
    private bool waitingAtBuild = false;
    private GameObject destinationMarker;

    void OnEnable()
    {
        startBehavior = true;
        interrupt = false;
        isHoldingWall = false;
        currentState = AgentState.Idle;
    }

    void OnDisable()
    {
        // simply disable carried visual and stop movement
        if (smallWall != null)
            smallWall.SetActive(false);
        isHoldingWall = false;
        navAgent.isStopped = true;
        if (destinationMarker)
            Destroy(destinationMarker);
    }

    void Update()
    {
        if (interrupt)
        {
            navAgent.isStopped = true;
            return;
        }

        if (startBehavior && currentState == AgentState.Idle)
            currentState = AgentState.MoveToHabitat;

        switch (currentState)
        {
            case AgentState.MoveToHabitat:
                navAgent.isStopped = false;
                navAgent.SetDestination(Habitat.Instance.transform.position);
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    currentState = AgentState.WaitAtHabitat;
                    StartCoroutine(WaitAtHabitatCoroutine());
                }
                break;

            case AgentState.MoveToBuildPosition:
                smallWall.SetActive(true);
                if (destinationMarker)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(destinationMarker.transform.position);
                    if (!navAgent.pathPending && navAgent.remainingDistance <= destinationStopDistance)
                    {
                        currentState = AgentState.WaitAtBuildPosition;
                        StartCoroutine(WaitAtBuildPositionCoroutine());
                    }
                }
                break;

            case AgentState.CreateWall:
                Habitat.Instance.BuildNextWall();
                Destroy(destinationMarker);
                smallWall.SetActive(false);
                currentState = AgentState.Idle;
                startBehavior = false;
                break;
        }
    }

    IEnumerator WaitAtHabitatCoroutine()
    {
        yield return new WaitForSeconds(1f);
        waitingAtHabitat = false;
        
        SetBuildData();
        currentState = AgentState.MoveToBuildPosition;
    }

    IEnumerator WaitAtBuildPositionCoroutine()
    {
        yield return new WaitForSeconds(3f);
        waitingAtBuild = false;
        currentState = AgentState.CreateWall;
    }

    /// <summary>
    /// Sets the build target XZ and spawns the destination prefab marker.
    /// </summary>
    public void SetBuildData()
    {
        var nextWall = Habitat.Instance.GetNextWallToBuild();
        Vector2 xz = new Vector2(nextWall.transform.position.x, nextWall.transform.position.z);
        if (destinationMarker)
            Destroy(destinationMarker);
        destinationMarker = SpawnDestinationMarker(xz);
    }

    /// <summary>
    /// Spawns the destination marker on the NavMesh at the requested XZ.
    /// </summary>
    private GameObject SpawnDestinationMarker(Vector2 xz)
    {
        Vector3 origin = new Vector3(xz.x, transform.position.y + 10f, xz.y);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origin, out hit, 20f, NavMesh.AllAreas))
            return Instantiate(destinationPrefab, hit.position, Quaternion.identity);

        Debug.LogWarning($"BuildWall_Agent: Cannot sample NavMesh at XZ=({xz.x},{xz.y})");
        return null;
    }
}
