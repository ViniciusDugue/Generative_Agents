using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BuildWall_Agent : MonoBehaviour
{
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
    public bool isHoldingWall; 

    [Header("Destination Settings")]
    public float destinationStopDistance = 1f;

    [Header("Control Booleans")]
    public bool startBehavior = false;
    public bool interrupt = false;

    public NavMeshAgent agent;

    // Agent States
    private enum AgentState
    {
        Idle,
        MoveToConstruction,
        WaitAtConstruction,
        CreateWall,
        MoveToWall,
        WaitBeforePickup,
        PickupWall,
        MoveToDestination,
        PlaceWall
    }

    private AgentState currentState = AgentState.Idle;
    private GameObject currentWallInstance;

    private bool waitingAtConstruction = false;
    private bool wasInterruptedDuringDestination = false;

    void OnEnable()
    {
        startBehavior =true; 
        interrupt = false;


    }

    void OnDisable()
    {
        currentState = AgentState.Idle;
        startBehavior = false;
        navAgent.isStopped = true;

        if(isHoldingWall)
        {
            isHoldingWall = false;
            currentWallInstance = Instantiate(wallPrefab, Destination.transform.position + new Vector3(0, 2f, 0), Quaternion.Euler(0, WallRotation, 0));
            currentState = AgentState.MoveToWall;
            smallWall.SetActive(false);
        }

    }

    void SetWallAgentData(Vector3 constructionSitePos, Vector3 destinationPos)
    {
        Destination = Instantiate(destinationPrefab,destinationPos, Quaternion.identity);
        ConstructionSite = Instantiate(constructionSitePrefab,constructionSitePos, Quaternion.identity);
    }

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
            agent.isStopped = true;
            agent.ResetPath();

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
                if (agent.destination != ConstructionSite.transform.position)
                {
                    agent.isStopped = false;
                    agent.SetDestination(ConstructionSite.transform.position);
                }
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
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
            case AgentState.MoveToWall
                MoveToWall();
                break;

            case AgentState.WaitBeforePickup:
                break;

            case AgentState.PickupWall:
                PickupWall();
                isHoldingWall = true;
                break;

            case AgentState.MoveToDestination:
                {
                    Vector3 toAgent = transform.position - Destination.transform.position;
                    Vector3 destinationTarget = Destination.transform.position;
                    if (toAgent != Vector3.zero)
                    {
                        destinationTarget += toAgent.normalized * destinationStopDistance;
                    }
                    if (agent.destination != destinationTarget)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(destinationTarget);
                    }
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        currentState = AgentState.PlaceWall;
                    }
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
        
        currentState = AgentState.MoveToWall;
    }

    private void MoveToWall()
    {

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
