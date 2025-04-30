using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Behavior to relocate an existing wall GameObject to a given XZ destination.
/// Implements a simple state machine with startBehavior, interrupt, and isHoldingWall flags:
/// Idle → MoveToWall → WaitForWallPick → PickupWall → MoveToDestination → PlaceWall.
/// </summary>
public class MoveWall_Behavior : AgentBehavior
{
    [Header("References")]
    public NavMeshAgent navAgent;
    public GameObject destinationPrefab;      // marker prefab
    public GameObject wallPrefab;            // final wall prefab

    [Header("Visuals")]
    public GameObject smallWall;             // carried-wall visual

    [Header("Parameters")]
    public float navSampleRadius = 20f;
    public float pickupRange = 1.5f;         // distance to pick up wall
    public float stoppingDistance = 1f;
    public float pickupDelay = 1f;

    [Header("Control Flags")]
    public bool startBehavior = false;
    public bool interrupt = false;
    public bool isHoldingWall = false;

    [Header("Initial Settings")]
    [Tooltip("Wall object to pick up when enabled")]
    public GameObject initialTargetWall;
    [Tooltip("XZ coordinates for destination marker when enabled")]  
    public Vector2 initialDestinationXZ;
    [Tooltip("Rotation for placed wall (degrees)")]
    public float initialWallRotation;

    private GameObject targetWall;
    private Vector2 destinationXZ;
    private float wallRotation;
    private GameObject destinationMarker;

    public enum AgentState
    {
        Idle,
        MoveToWall,
        WaitForWallPick,
        PickupWall,
        MoveToDestination,
        PlaceWall
    }
    public AgentState currentState = AgentState.Idle;

    public static MoveWall_Behavior Instance;

    void Awake()
    {
        Instance = this;
        if (smallWall != null)
            smallWall.SetActive(false);
    }

    void OnEnable()
    {
        // reset flags and state
        startBehavior = true;
        interrupt = false;
        isHoldingWall = false;
        currentState = AgentState.Idle;

        // initialize data
        if (initialTargetWall != null)
            SetMoveWallData(initialTargetWall, initialDestinationXZ, initialWallRotation);
    }

    void OnDisable()
    {
        // drop carried wall if interrupt or disable
        if (isHoldingWall)
        {
            smallWall?.SetActive(false);
            isHoldingWall = false;
            Vector3 dropPos = transform.position + transform.forward * pickupRange;
            if (wallPrefab != null)
                Instantiate(wallPrefab, dropPos, Quaternion.Euler(0, wallRotation, 0));
        }
        // cleanup
        currentState = AgentState.Idle;
        startBehavior = false;
        interrupt = false;
        navAgent.isStopped = true;
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
            destinationMarker = null;
        }
    }

    /// <summary>
    /// Sets the wall to move, destination XZ, and rotation.
    /// Only stores data and creates the marker.
    /// </summary>
    public void SetMoveWallData(GameObject wallObject, Vector2 xzCoords, float rotation)
    {
        targetWall = wallObject;
        destinationXZ = xzCoords;
        wallRotation = rotation;

        // cleanup old marker
        if (destinationMarker != null)
            Destroy(destinationMarker);

        // spawn new marker on NavMesh
        destinationMarker = SpawnDestinationAt(destinationXZ);
    }

    void Update()
    {
        // Handle interrupt
        if (interrupt)
        {
            navAgent.isStopped = true;
            return;
        }

        // start when data is set
        if (startBehavior && currentState == AgentState.Idle && targetWall != null && destinationMarker != null)
        {
            currentState = AgentState.MoveToWall;
            navAgent.isStopped = false;
            navAgent.stoppingDistance = pickupRange;
            navAgent.SetDestination(targetWall.transform.position);
        }

        switch (currentState)
        {
            case AgentState.Idle:
                break;

            case AgentState.MoveToWall:
                if (!navAgent.pathPending && navAgent.remainingDistance <= pickupRange)
                {
                    currentState = AgentState.WaitForWallPick;
                    StartCoroutine(WaitForWallPickCoroutine());
                }
                break;

            case AgentState.WaitForWallPick:
                break;

            case AgentState.PickupWall:
                // destroy original and show smallWall
                if (targetWall != null && smallWall != null)
                {
                    Destroy(targetWall);
                    targetWall = null;
                    smallWall.SetActive(true);
                    isHoldingWall = true;
                }
                currentState = AgentState.MoveToDestination;
                navAgent.stoppingDistance = stoppingDistance;
                navAgent.SetDestination(destinationMarker.transform.position);
                break;

            case AgentState.MoveToDestination:
                if (!navAgent.pathPending && navAgent.remainingDistance <= stoppingDistance)
                {
                    currentState = AgentState.PlaceWall;
                }
                break;

            case AgentState.PlaceWall:
                // place final wall
                if (wallPrefab != null)
                {
                    Instantiate(wallPrefab, destinationMarker.transform.position, Quaternion.Euler(0, wallRotation, 0));
                }
                smallWall?.SetActive(false);
                isHoldingWall = false;

                Destroy(destinationMarker);
                destinationMarker = null;
                navAgent.isStopped = true;
                currentState = AgentState.Idle;
                startBehavior = false;
                break;
        }
    }

    private IEnumerator WaitForWallPickCoroutine()
    {
        yield return new WaitForSeconds(pickupDelay);
        if (!interrupt)
            currentState = AgentState.PickupWall;
    }

    private GameObject SpawnDestinationAt(Vector2 xz)
    {
        Vector3 origin = new Vector3(xz.x, transform.position.y + 10f, xz.y);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origin, out hit, navSampleRadius, NavMesh.AllAreas))
        {
            return Instantiate(destinationPrefab, hit.position, Quaternion.identity);
        }
        Debug.LogWarning($"MoveWall_Behavior: Could not sample NavMesh at XZ=({xz.x}, {xz.y})");
        return null;
    }
}
