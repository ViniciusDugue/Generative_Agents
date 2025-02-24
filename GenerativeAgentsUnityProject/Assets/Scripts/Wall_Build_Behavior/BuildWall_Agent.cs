using UnityEngine;
using System.Collections;

public class BuildWall_Agent : MonoBehaviour
{
    // Inspector-assigned variables
    [Header("Required GameObjects")]
    public GameObject[] n_blocks_required;
    public GameObject wallPrefab;
    public GameObject smallWall;
    public GameObject ConstructionSite;
    public GameObject Destination;

    [Header("Rotation Settings")]
    public float WallRotation;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 5f;
    public float rotationSpeed = 5f;
    public float rangeThreshold = 1f;
    public float pickupRange = 2f;

    [Header("Destination Settings")]
    public float destinationStopDistance = 1f;

    [Header("Control Booleans")]
    public bool startBehavior = false;
    public bool interrupt = false;

    // Agent States
    private enum AgentState
    {
        Idle,
        MoveToConstruction,
        WaitAtConstruction,
        CreateWall,
        WaitBeforePickup,
        PickupWall,
        MoveToDestination,
        PlaceWall
    }

    private AgentState currentState = AgentState.Idle;
    private GameObject currentWallInstance;

    private float currentSpeed = 0f;
    private bool waitingAtConstruction = false;
    private bool wasInterruptedDuringDestination = false;

    void Update()
    {
        //interrupt check: if interrupt true, stop all behavior.
        if (interrupt)
        {
            if (currentState == AgentState.MoveToDestination)
            {
                HandleInterruptDuringDestination();
                smallWall.SetActive(false);
                wasInterruptedDuringDestination = true;
            }
            currentState = AgentState.Idle;
            return;
        }
        
        if (wasInterruptedDuringDestination && !interrupt)
        {
            if (currentWallInstance != null)
            {
                Destroy(currentWallInstance);
            }
            smallWall.SetActive(true);

            currentState = AgentState.MoveToDestination;
            wasInterruptedDuringDestination = false;
        }

        if (startBehavior && currentState == AgentState.Idle)
        {
            currentState = AgentState.MoveToConstruction;
        }

        switch (currentState)
        {
            case AgentState.MoveToConstruction:
                MoveToTarget(ConstructionSite.transform.position);
                if (Vector3.Distance(transform.position, ConstructionSite.transform.position) <= rangeThreshold)
                {
                    currentSpeed = 0f;
                    currentState = AgentState.WaitAtConstruction;
                }
                break;

            case AgentState.WaitAtConstruction:
                if (!waitingAtConstruction)
                {
                    waitingAtConstruction = true;
                    StartCoroutine(WaitAtConstructionCoroutine());
                }
                break;

            case AgentState.CreateWall:
                CreateWall();
                break;

            case AgentState.WaitBeforePickup:
                break;

            case AgentState.PickupWall:
                PickupWall();
                break;

            case AgentState.MoveToDestination:
                {
                    Vector3 toAgent = transform.position - Destination.transform.position;
                    Vector3 destinationTarget = Destination.transform.position;
                    if (toAgent != Vector3.zero)
                    {
                        destinationTarget += toAgent.normalized * destinationStopDistance;
                    }
                    MoveToTarget(destinationTarget);
                    if (Vector3.Distance(transform.position, destinationTarget) <= rangeThreshold)
                    {
                        currentSpeed = 0f;
                        currentState = AgentState.PlaceWall;
                    }
                }
                break;

            case AgentState.PlaceWall:
                PlaceWall();
                break;

            case AgentState.Idle:
            default:
                break;
        }
    }

    //movement function for moving to target whether thats the ConstructionSite or Destination
    private void MoveToTarget(Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - transform.position;
        float distance = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (distance > rangeThreshold)
        {
            currentSpeed = Mathf.Min(moveSpeed, currentSpeed + acceleration * Time.deltaTime);
            transform.position += direction * currentSpeed * Time.deltaTime;
        }
        else
        {
            transform.position = targetPos;
            currentSpeed = 0f;
        }
    }
    // wait 1 second at construction before creating the wall
    IEnumerator WaitAtConstructionCoroutine()
    {
        yield return new WaitForSeconds(1f);
        waitingAtConstruction = false;
        currentState = AgentState.CreateWall;
    }

    private void CreateWall()
    {
        foreach (GameObject block in n_blocks_required)
        {
            if (block != null)
                Destroy(block);
        }
        currentWallInstance = Instantiate(wallPrefab, ConstructionSite.transform.position, Quaternion.Euler(0, WallRotation, 0));
        StartCoroutine(WaitAndPickup());
        currentState = AgentState.WaitBeforePickup;
    }

    // wait 1 second before picking up wall
    IEnumerator WaitAndPickup()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.PickupWall;
    }

    private void PickupWall()
    {
        if (currentWallInstance != null && Vector3.Distance(transform.position, currentWallInstance.transform.position) <= pickupRange)
        {
            Destroy(currentWallInstance);
            Quaternion desiredWorldRot = Quaternion.Euler(0, WallRotation, 0);
            smallWall.transform.localRotation = Quaternion.Inverse(transform.rotation) * desiredWorldRot;
            smallWall.SetActive(true);
        }
        currentState = AgentState.MoveToDestination;
    }

    // if interrupt while moving to destination, drop the wall
    private void HandleInterruptDuringDestination()
    {
        if (currentWallInstance != null)
        {
            Destroy(currentWallInstance);
        }
        Vector3 frontPos = transform.position + transform.forward;
        currentWallInstance = Instantiate(wallPrefab, frontPos, Quaternion.Euler(0, WallRotation, 0));
        currentState = AgentState.Idle;
    }

    // place wall at destination if in range.
    private void PlaceWall()
    {
        if (currentWallInstance != null)
        {
            Destroy(currentWallInstance);
        }

        currentWallInstance = Instantiate(wallPrefab, Destination.transform.position + new Vector3(0, 2f, 0), Quaternion.Euler(0, WallRotation, 0));
        smallWall.SetActive(false);
        currentState = AgentState.Idle;
        startBehavior = false;
    }
}
