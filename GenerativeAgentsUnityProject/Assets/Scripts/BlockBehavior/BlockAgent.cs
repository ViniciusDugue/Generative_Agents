using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BlockAgent : MonoBehaviour
{
    public enum AgentState
    {
        Idle,
        MoveToBlock,
        WaitBeforePickup,
        PickUpBlock,
        WaitAfterPickup,
        MoveToDestination,
        WaitBeforeDrop,
        DropBlock
    }

    [Header("References")]
    public GameObject targetBlock;
    public GameObject destinationObject;
    public NavMeshAgent navAgent;

    [Header("Behavior Settings")]
    public bool startBehavior = false;
    public bool interrupt = false;
    public float pickupRange = 2f;
    public float destinationStopDistance = 1f;

    private bool dropAtFront = false;

    public AgentState currentState = AgentState.Idle;

    void Start()
    {
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
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

            case AgentState.PickUpBlock:
                if (targetBlock != null)
                {
                    targetBlock.transform.SetParent(transform);
                    targetBlock.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                }
                currentState = AgentState.WaitAfterPickup;
                StartCoroutine(WaitAfterPickupCoroutine());
                break;

            case AgentState.WaitAfterPickup:
                break;

            case AgentState.MoveToDestination:
                if (destinationObject != null)
                {
                    navAgent.SetDestination(destinationObject.transform.position);
                    float distToDestination = Vector3.Distance(transform.position, destinationObject.transform.position);
                    if (distToDestination <= destinationStopDistance && !interrupt)
                    {
                        currentState = AgentState.WaitBeforeDrop;
                        StartCoroutine(WaitBeforeDropCoroutine());
                    }
                }
                break;

            case AgentState.WaitBeforeDrop:
                break;

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
    IEnumerator WaitBeforePickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.PickUpBlock;
    }
    IEnumerator WaitAfterPickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.MoveToDestination;
        if (destinationObject != null)
        {
            navAgent.SetDestination(destinationObject.transform.position);
        }
    }
    IEnumerator WaitBeforeDropCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.DropBlock;
    }
}

