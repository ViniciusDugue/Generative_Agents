using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BuildWall_Agent : AgentBehavior
{
    [Header("Required GameObjects")]
    public GameObject smallWall;
    public GameObject destinationPrefab;
    public GameObject wallPrefab;

    [Header("Rotation Settings")]
    public float wallRotation;
    public Vector2 destinationXZ;

    [Header("Movement Settings")]
    public NavMeshAgent navAgent;
    public float pickupRange = 2f;
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
        CreateWall,
        MoveToDestination,
        PlaceWall
    }
    public AgentState currentState = AgentState.Idle;

    private bool waitingAtHabitat = false;
    private bool wasInterruptedDuringDestination = false;
    private GameObject destinationMarker;

    public static BuildWall_Agent Instance;

    void Awake()
    {
        Instance = this;
        smallWall?.SetActive(false);
    }

    void OnEnable()
    {
        startBehavior = true;
        interrupt = false;
        SetWallData(destinationXZ, wallRotation);
    }

    void OnDisable()
    {
        currentState = AgentState.Idle;
        startBehavior = false;
        navAgent.isStopped = true;

        if (isHoldingWall)
        {
            smallWall.SetActive(false);
            isHoldingWall = false;
            Vector3 dropPos = transform.position + transform.forward * 1.5f;
            Instantiate(wallPrefab, dropPos, Quaternion.Euler(0, wallRotation, 0));
        }
    }

    /// <summary>
    /// Call this to set where the wall should be placed (XZ) and its rotation.
    /// </summary>
    public void SetWallData(Vector2 destinationXZ, float WallRotation)
    {
        wallRotation = WallRotation;
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
        }
        destinationMarker = SpawnDestinationMarker(destinationXZ);
    }

    /// <summary>
    /// Samples the NavMesh at the given XZ and spawns the destination prefab.
    /// Returns the instantiated marker or null if sample fails.
    /// </summary>
    private GameObject SpawnDestinationMarker(Vector2 xz)
    {
        Vector3 sampleOrigin = new Vector3(xz.x, transform.position.y + 10f, xz.y);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(sampleOrigin, out hit, 20f, NavMesh.AllAreas))
        {
            return Instantiate(
                destinationPrefab,
                hit.position,
                Quaternion.Euler(0, wallRotation, 0)
            );
        }
        else
        {
            Debug.LogWarning($"BuildWall_Agent: Unable to find NavMesh at XZ=({xz.x}, {xz.y})");
            return null;
        }
    }

    void Update()
    {
        if (interrupt)
        {
            if (currentState == AgentState.MoveToDestination)
            {
                wasInterruptedDuringDestination = true;
                smallWall.SetActive(false);
                isHoldingWall = false;
            }
            navAgent.isStopped = true;
            navAgent.ResetPath();
            currentState = AgentState.Idle;
            return;
        }

        if (wasInterruptedDuringDestination && !interrupt)
        {
            smallWall.SetActive(true);
            isHoldingWall = true;
            currentState = AgentState.MoveToDestination;
            wasInterruptedDuringDestination = false;
        }

        if (startBehavior && currentState == AgentState.Idle)
            currentState = AgentState.MoveToHabitat;

        switch (currentState)
        {
            case AgentState.MoveToHabitat:
                if (Habitat.Instance != null)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(Habitat.Instance.transform.position);
                    if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                        currentState = AgentState.WaitAtHabitat;
                }
                break;

            case AgentState.WaitAtHabitat:
                if (!waitingAtHabitat)
                {
                    waitingAtHabitat = true;
                    StartCoroutine(WaitAtHabitatCoroutine());
                }
                break;

            case AgentState.CreateWall:
                if (Habitat.Instance.storedBlocks >= 3)
                {
                    Habitat.Instance.storedBlocks -= 3;
                    EndSimMetricsUI.Instance.IncrementWallsBuilt();

                    smallWall.SetActive(true);
                    isHoldingWall = true;

                    currentState = AgentState.MoveToDestination;
                }
                else
                {
                    Debug.Log("Not enough blocks at habitat");
                    currentState = AgentState.WaitAtHabitat;
                    waitingAtHabitat = false;
                }
                break;

            case AgentState.MoveToDestination:
                if (destinationMarker != null)
                {
                    Vector3 toDst = transform.position - destinationMarker.transform.position;
                    Vector3 dstTarget = destinationMarker.transform.position + toDst.normalized * destinationStopDistance;
                    navAgent.isStopped = false;
                    navAgent.SetDestination(dstTarget);

                    if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                        currentState = AgentState.PlaceWall;
                }
                break;

            case AgentState.PlaceWall:
                isHoldingWall = false;
                PlaceWall();
                break;

            case AgentState.Idle:
            default:
                break;
        }
    }

    // 1s pause before CreateWall
    IEnumerator WaitAtHabitatCoroutine()
    {
        yield return new WaitForSeconds(1f);
        waitingAtHabitat = false;
        currentState = AgentState.CreateWall;
    }

    private void PlaceWall()
    {
        EndSimMetricsUI.Instance.IncrementWallsPlaced();
        Instantiate(
            wallPrefab,
            destinationMarker.transform.position + Vector3.up * 2f,
            Quaternion.Euler(0, wallRotation, 0)
        );
        Destroy(destinationMarker);
        smallWall.SetActive(false);
        currentState = AgentState.Idle;
        startBehavior = false;
    }
}
