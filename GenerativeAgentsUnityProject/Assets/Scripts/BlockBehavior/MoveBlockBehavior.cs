using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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

    [Header("Block")]    
    public GameObject blockPrefab;
    public GameObject targetBlock;
    public bool isHoldingBlock;

    [Header("Agent Settings")]
    public NavMeshAgent navAgent;
    public float pickupRange = 2f;
    public float destinationStopDistance = 1f;

    [Header("Visuals")]
    public GameObject holdingBlock;

    [Header("Control Flags")]
    private bool startBehavior = false;
    private bool interrupt = false;
    public AgentState currentState = AgentState.Idle;

    public static MoveBlockBehavior Instance;

    void Awake()
    {
        Instance = this;
        holdingBlock?.SetActive(false);
    }

    void OnEnable()
    {
        startBehavior = true;
        interrupt = false;

        SetBlockAgentData(null);
    }

    void OnDisable()
    {
        // Reset state and stop agent
        currentState = AgentState.Idle;
        startBehavior = false;
        interrupt = false;
        if (navAgent.isOnNavMesh)
            navAgent.isStopped = true;
        if(isHoldingBlock)
        {
            Habitat.Instance.storedBlocks++;
            EndSimMetricsUI.Instance.IncrementBlocksMoved();
        }
        
        // Hide holding visual
        holdingBlock?.SetActive(false);
        isHoldingBlock = false;

        // // Spawn a new block in front of the agent
        // if (targetBlock != null)
        // {
        //     Vector3 spawnPos = transform.position + transform.forward * pickupRange;
        //     Instantiate(blockPrefab, spawnPos, Quaternion.identity);
        // }
    }

    /// <summary>
    /// Assign which block to fetch; the agent will auto‐deliver to Habitat.Instance.
    /// </summary>
    public void SetBlockAgentData(GameObject targetBlockObject)
    {
        if (targetBlockObject != null)
        {
            targetBlock = targetBlockObject;
            return;
        }

        // No block explicitly passed — search for closest from BehaviorManager
        BehaviorManager manager = GetComponent<BehaviorManager>();
        if (manager == null || manager.blockMappingsList.Count == 0)
        {
            Debug.LogWarning($"[MoveBlockBehavior] No BehaviorManager or discovered blocks on agent {gameObject.name}");
            return;
        }

        GameObject closestBlock = null;
        float closestDistance = float.MaxValue;

        foreach (var entry in manager.blockMappingsList)
        {
            if (entry.blockObject == null) continue;

            float distance = Vector3.Distance(transform.position, entry.blockObject.transform.position);
            if (distance < closestDistance)
            {
                closestBlock = entry.blockObject;
                closestDistance = distance;
            }
        }

        if (closestBlock != null)
        {
            targetBlock = closestBlock;
            Debug.Log($"[MoveBlockBehavior] Closest discovered block '{closestBlock.name}' assigned to {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[MoveBlockBehavior] No valid block found for {gameObject.name}");
        }
    }

    void Update()
    {
        // Honor interruption flag
        navAgent.isStopped = interrupt;
        if (interrupt)
            return;

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
                if (targetBlock != null)
                {
                    Destroy(targetBlock);
                    targetBlock = null;
                    holdingBlock?.SetActive(true);
                    isHoldingBlock= true;
                }
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
                holdingBlock?.SetActive(false);
                Habitat.Instance.storedBlocks++;
                EndSimMetricsUI.Instance.IncrementBlocksMoved();
                currentState = AgentState.Idle;
                startBehavior = false;
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
