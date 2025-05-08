using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BuildWallBehavior : AgentBehavior
{
    public enum AgentState
    {
        Idle,
        MoveToHabitat,
        WaitAtHabitat,
        MoveToBuildPosition,
        WaitAtBuildPosition,
        CreateWall
    }

    [Header("Required GameObjects")]
    [Tooltip("Visual prefab of the half-built wall carried by the agent.")]
    public GameObject smallWall;
    [Tooltip("Destination marker prefab for where to build.")]
    public GameObject destinationPrefab;

    [Header("Agent Settings")]
    public NavMeshAgent navAgent;
    public float destinationStopDistance = 1f;

    [Header("Build Sequence")]
    [Tooltip("Number of walls to build in total.")]
    public int totalWalls = 4;

    [HideInInspector] public bool completeBuilding = false;

    private int wallsBuilt = 0;
    private bool startBehavior = false;
    private bool interrupt = false;
    private AgentState currentState = AgentState.Idle;
    private GameObject destinationMarker;

    void OnEnable()
    {
        
        wallsBuilt = 0;
        completeBuilding = false;
        interrupt = false;
        startBehavior = true;
        smallWall?.SetActive(false);
        currentState = AgentState.Idle;
    }

    void OnDisable()
    {
        // cleanup
        if (smallWall != null)
            smallWall.SetActive(false);
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

        // kick off first move
        if (startBehavior && currentState == AgentState.Idle)
        {
            AgentBehaviorUI.Instance.UpdateAgentBehaviorUI("BuildWallBehavior");
            currentState = AgentState.MoveToHabitat;
        }
            

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
                // perform build
                Habitat.Instance.BuildNextWall();
                Destroy(destinationMarker);
                smallWall.SetActive(false);

                wallsBuilt++;
                if (wallsBuilt < totalWalls)
                {
                    // queue the next one
                    startBehavior = true;
                    currentState = AgentState.Idle;
                }
                else
                {
                    // all done
                    completeBuilding = true;
                    this.enabled = false;
                }
                break;
        }
    }

    private IEnumerator WaitAtHabitatCoroutine()
    {
        yield return new WaitForSeconds(1f);
        startBehavior = false;
        SetBuildData();
        currentState = AgentState.MoveToBuildPosition;
    }

    private IEnumerator WaitAtBuildPositionCoroutine()
    {
        yield return new WaitForSeconds(3f);
        currentState = AgentState.CreateWall;
    }

    /// <summary>
    /// Determines where the next wall goes and places a marker there.
    /// </summary>
    private void SetBuildData()
    {
        var nextWall = Habitat.Instance.GetNextWallToBuild();
        Vector2 xz = new Vector2(nextWall.transform.position.x, nextWall.transform.position.z);

        if (destinationMarker)
            Destroy(destinationMarker);
        destinationMarker = SpawnDestinationMarker(xz);
    }

    private GameObject SpawnDestinationMarker(Vector2 xz)
    {
        Vector3 origin = new Vector3(xz.x, transform.position.y + 10f, xz.y);
        if (NavMesh.SamplePosition(origin, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            return Instantiate(destinationPrefab, hit.position, Quaternion.identity);

        Debug.LogWarning($"BuildWallBehavior: Cannot sample NavMesh at XZ=({xz.x},{xz.y})");
        return null;
    }
}
