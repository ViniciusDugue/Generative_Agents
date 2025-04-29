using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MoveBlockBehavior : AgentBehavior
{
    // enum table for all block agent behavior states
    public enum AgentState
    {
        Idle,
        MoveToBlock,
        WaitBeforePickup,
        PickUpBlock,
        WaitAfterPickup,
        MoveToDestination,
        DropBlock
    }

    public GameObject targetBlock;
    public GameObject destinationObject;
    public NavMeshAgent navAgent;
    public bool startBehavior = false;
    public bool interrupt = false;
    public float pickupRange = 2f;
    public bool isHoldingBlock;
    public float destinationStopDistance = 1f;
    public float distToDestination;
    private bool dropAtFront = false;
    public GameObject destinationPrefab;
    public AgentState currentState = AgentState.Idle;

    public GameObject holdingBlock;
    public GameObject blockPrefab;

    public static MoveBlockBehavior Instance;

    protected override void Awake()
    {
        Instance = this;
    }

    protected override void OnEnable()
    {
        startBehavior = true;
        interrupt = false;
    }

    protected override void OnDisable()
    {
        // not sure if you want an agent to continue its task after being interrupted.
        currentState = AgentState.Idle;
        startBehavior = false;

        interrupt = false;
        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
        else
        {
            Debug.LogWarning("OnDisable: NavMeshAgent is not on a NavMesh.");
        }

        if (isHoldingBlock)
        {
<<<<<<< HEAD
=======
            EndSimMetricsUI.Instance.IncrementBlocksMoved();
>>>>>>> main
            holdingBlock?.SetActive(false);
            targetBlock = Instantiate(blockPrefab, transform.position + transform.forward + new Vector3(0f,1f,0f), Quaternion.identity);
            isHoldingBlock = false;
        }
    
        // navAgent.SetDestination(transform.position);
    }

    public void SetBlockAgentData(GameObject targetBlockObject, Vector3 destinationPos)
    {
        targetBlock = targetBlockObject;
        destinationObject = Instantiate(destinationPrefab, destinationPos,Quaternion.identity);

    }
    void Update()
    {

        if (interrupt)
        {
            navAgent.isStopped = true;
            if (currentState == AgentState.MoveToDestination)
            {
                dropAtFront = true;
            }
        }
        else
        {
            navAgent.isStopped = false;
        }
        switch (currentState)
        {
            // idle position for start and interrupt
            case AgentState.Idle:
                if (startBehavior)
                {
                    currentState = AgentState.MoveToBlock;
                    if (targetBlock != null)
                    {
                        navAgent.SetDestination(targetBlock.transform.position);
                    }
                }
                break;
            // move towards the block
            case AgentState.MoveToBlock:
                if (targetBlock != null)
                {
                    navAgent.SetDestination(targetBlock.transform.position);
                    float distToBlock = Vector3.Distance(transform.position, targetBlock.transform.position);
                    if (distToBlock <= pickupRange && !interrupt)
                    {
                        currentState = AgentState.WaitBeforePickup;
                        StartCoroutine(WaitBeforePickupCoroutine());
                    }
                }
                break;
            case AgentState.WaitBeforePickup:
                break;
            // pick up the block
            case AgentState.PickUpBlock:
                // holds the block above agent
                if (targetBlock != null)
                {
                    Destroy(targetBlock);
                    targetBlock = null;

                    holdingBlock?.SetActive(true);
                }

                isHoldingBlock = true;
                currentState = AgentState.WaitAfterPickup;
                StartCoroutine(WaitAfterPickupCoroutine());
                break;
            //wait after pickup
            case AgentState.WaitAfterPickup:
                break;
            case AgentState.MoveToDestination:
                if (destinationObject != null)
                {
                    navAgent.SetDestination(destinationObject.transform.position);
                    distToDestination = Vector3.Distance(transform.position, destinationObject.transform.position);
                    if (distToDestination <= destinationStopDistance && !interrupt)
                    {
                        currentState = AgentState.DropBlock;
                    }
                }
                break;
            // drop block by swetting to position of destination
            case AgentState.DropBlock:
                holdingBlock?.SetActive(false);
<<<<<<< HEAD

=======
                
                EndSimMetricsUI.Instance.IncrementBlocksMoved();
>>>>>>> main
                if (dropAtFront)
                {
                    isHoldingBlock = false;
                    targetBlock = Instantiate(blockPrefab, transform.position + transform.forward+ new Vector3(0f,1f,0f), Quaternion.identity);
                }
                else if (destinationObject != null)
                {   
                    isHoldingBlock = false;
                    targetBlock = Instantiate(blockPrefab, transform.position + transform.forward+ new Vector3(0f,1f,0f), Quaternion.identity);
                }
                currentState = AgentState.Idle;
                startBehavior = false;
                dropAtFront = false;
                break;
        }
    }

    //wait before picking up block
    IEnumerator WaitBeforePickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.PickUpBlock;
    }
    // wait coroutine after picking up block
    IEnumerator WaitAfterPickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.MoveToDestination;
        if (destinationObject != null)
        {
            navAgent.SetDestination(destinationObject.transform.position);
        }
    }
}

