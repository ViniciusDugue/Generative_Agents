using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BuildWall_Agent : AgentBehavior
{
    [Header("Required GameObjects")]
    public GameObject[] n_blocks_required;
    
    public GameObject smallWall;
    public GameObject ConstructionSite;
    public GameObject Destination;

    public GameObject destinationPrefab;
    public GameObject wallPrefab;
    public GameObject constructionSitePrefab;

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

    public NavMeshAgent navAgent;

    // Agent States
    public enum AgentState
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

    public AgentState currentState = AgentState.Idle;
    private GameObject currentWallInstance;

    private bool waitingAtConstruction = false;
    private bool wasInterruptedDuringDestination = false;

    public static BuildWall_Agent Instance;

    void Awake()
    {
        Instance = this;
    }
    void OnEnable()
    {
        startBehavior =true; 
        interrupt = false;
        // This will be called by llm to set initial values for behavior
        //SetWallAgentData(constructionSitePos, destinationPos, targetBlockList);
    }

    void OnDisable()
    {
        currentState = AgentState.Idle;
        startBehavior = false;
        navAgent.isStopped = true;

        if(isHoldingWall)
        {
            smallWall.SetActive(false);
            isHoldingWall = false;
            //These will be necessary for the llm integration for interrupting behavior
            // Destroy(Destination);
            // Destroy(ConstructionSite);

            currentWallInstance = Instantiate(wallPrefab, transform.position + transform.forward * 1.5f, Quaternion.Euler(0, WallRotation, 0));
            currentState = AgentState.MoveToWall;
            
            
        }

    }

    public void SetWallAgentData(Vector3 constructionSitePos, Vector3 destinationPos, GameObject[] targetBlockList)
    {
        Destination = Instantiate(destinationPrefab,destinationPos, Quaternion.identity);
        ConstructionSite = Instantiate(constructionSitePrefab,constructionSitePos, Quaternion.identity);
        n_blocks_required = targetBlockList;
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
            navAgent.isStopped = true;
            navAgent.ResetPath();

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
                if (navAgent.destination != ConstructionSite.transform.position)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(ConstructionSite.transform.position);
                }
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
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
            case AgentState.MoveToWall:
                {
                    if (currentWallInstance != null)
                    {
                        Vector3 toWall = transform.position - currentWallInstance.transform.position;
                        Vector3 targetPosition = currentWallInstance.transform.position;
                        if (toWall != Vector3.zero)
                        {
                            targetPosition += toWall.normalized * destinationStopDistance;
                        }
                        if (navAgent.destination != targetPosition)
                        {
                            navAgent.isStopped = false;
                            navAgent.SetDestination(targetPosition);
                        }
                        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                        {
                            StartCoroutine(WaitAndPickup());
                            currentState = AgentState.WaitBeforePickup;
                        }
                    }
                    break;
                }
                

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
                    if (navAgent.destination != destinationTarget)
                    {
                        navAgent.isStopped = false;
                        navAgent.SetDestination(destinationTarget);
                    }
                    if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
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
            Debug.Log("Ready to Pick up");
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
