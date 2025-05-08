using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class MoveBlockBehavior : AgentBehavior
{
    public enum AgentState
    {
        Idle,
        MoveToBlock,
        WaitBeforePickup,
        PickUpBlock,
        WaitAfterPickup,
        MoveToHabitat,
        DropBlockAtHabitat
    }

    [Header("Blocks To Move")]
    [Tooltip("Assign all blocks that should be fetched in order.")]
    public List<GameObject> targetBlocks = new List<GameObject>();

    [Header("Agent Settings")]
    public NavMeshAgent navAgent;
    public float pickupRange = 2f;
    public float destinationStopDistance = 1f;
    public GameObject targetBlock;
    [Header("Visuals")]
    public GameObject holdingBlock;

    [Header("Build Behavior")]
    [Tooltip("Once all blocks are moved, this BuildWallBehavior will be enabled.")]
    public BuildWallBehavior buildWallBehaviorToActivate;

    [HideInInspector] public bool completeDepositing = false;

    private int currentBlockIndex = 0;
    private AgentState currentState = AgentState.Idle;
    private bool startBehavior = false;
    private bool interrupt = false;

    public static MoveBlockBehavior Instance;

    void Awake()
    {
        AgentBehaviorUI.Instance.UpdateAgentBehaviorUI("MoveBlockBehavior");
        Instance = this;
        holdingBlock?.SetActive(false);
    }

    void OnEnable()
    {
        // reset indices and flags
        currentBlockIndex = 0;
        completeDepositing = false;
        interrupt = false;
        startBehavior = true;
        currentState = AgentState.Idle;

        // hide visuals
        holdingBlock?.SetActive(false);

        // set first target
        if (targetBlocks != null && targetBlocks.Count > 0)
            targetBlock = targetBlocks[0];
    }

    void OnDisable()
    {
        // reset navAgent
        if (navAgent.isOnNavMesh)
            navAgent.isStopped = true;
        // hide visuals
        holdingBlock?.SetActive(false);
    }

    void Update()
    {
        // respect interrupt
        navAgent.isStopped = interrupt;
        if (interrupt) return;

        switch (currentState)
        {
            case AgentState.Idle:
                if (startBehavior && targetBlock != null)
                {
                    currentState = AgentState.MoveToBlock;
                    navAgent.SetDestination(targetBlock.transform.position);
                }
                break;

            case AgentState.MoveToBlock:
                if (targetBlock != null)
                {
                    navAgent.SetDestination(targetBlock.transform.position);
                    if (Vector3.Distance(transform.position, targetBlock.transform.position) <= pickupRange)
                    {
                        currentState = AgentState.WaitBeforePickup;
                        StartCoroutine(WaitBeforePickupCoroutine());
                    }
                }
                break;

            case AgentState.WaitBeforePickup:
                // waiting…
                break;

            case AgentState.PickUpBlock:
                Destroy(targetBlock);
                holdingBlock?.SetActive(true);
                currentState = AgentState.WaitAfterPickup;
                StartCoroutine(WaitAfterPickupCoroutine());
                break;

            case AgentState.WaitAfterPickup:
                // waiting…
                break;

            case AgentState.MoveToHabitat:
                if (Habitat.Instance != null)
                {
                    Vector3 habitatPos = Habitat.Instance.transform.position;
                    navAgent.SetDestination(habitatPos);
                    if (Vector3.Distance(transform.position, habitatPos) <= destinationStopDistance)
                        currentState = AgentState.DropBlockAtHabitat;
                }
                break;

            case AgentState.DropBlockAtHabitat:
                // drop visual
                holdingBlock?.SetActive(false);
                Habitat.Instance.storedBlocks++;
                EndSimMetricsUI.Instance.IncrementBlocksMoved();

                // move to next block or finish
                currentBlockIndex++;
                if (currentBlockIndex < targetBlocks.Count)
                {
                    // set up next
                    targetBlock = targetBlocks[currentBlockIndex];
                    startBehavior = true;
                    currentState = AgentState.Idle;
                }
                else
                {
                    // all done
                    completeDepositing = true;
                    this.enabled = false;
                    if (buildWallBehaviorToActivate != null)
                        buildWallBehaviorToActivate.enabled = true;
                }
                break;
        }
    }

    private IEnumerator WaitBeforePickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.PickUpBlock;
    }

    private IEnumerator WaitAfterPickupCoroutine()
    {
        yield return new WaitForSeconds(1f);
        currentState = AgentState.MoveToHabitat;
    }
}
