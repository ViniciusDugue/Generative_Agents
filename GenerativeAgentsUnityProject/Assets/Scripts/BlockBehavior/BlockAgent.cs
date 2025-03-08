using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BlockAgent : MonoBehaviour
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
    public float destinationStopDistance = 1f;
    private bool dropAtFront = false;
    public AgentState currentState = AgentState.Idle;

    void Update()
    {
        // holds the block above agent
        if (targetBlock != null && targetBlock.transform.parent == transform)
        {
            targetBlock.transform.position = transform.position + new Vector3(0f, 1.5f, 0f);
        }
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
                if (targetBlock != null)
                {
                    targetBlock.transform.SetParent(transform);
                }
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
                    float distToDestination = Vector3.Distance(transform.position, destinationObject.transform.position);
                    if (distToDestination <= destinationStopDistance && !interrupt)
                    {
                        currentState = AgentState.DropBlock;
                    }
                }
                break;
            // drop block by swetting to position of destination
            case AgentState.DropBlock:
                if (targetBlock != null)
                {
                    targetBlock.transform.SetParent(null);
                    if (dropAtFront)
                    {
                        targetBlock.transform.position = transform.position + transform.forward;
                    }
                    else if (destinationObject != null)
                    {
                        targetBlock.transform.position = destinationObject.transform.position;
                    }
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
