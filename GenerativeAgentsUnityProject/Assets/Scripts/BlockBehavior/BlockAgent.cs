using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System.Collections.Generic; 

public class BlockAgent : Agent
{
    public BlockEnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    [SerializeField] public BlockSpawner blockSpawner;
    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public Material normalMaterial;
    public GameObject myLaser;

    [SerializeField] public Vector2 targetBlockPos;
    [SerializeField] public Vector2 targetBlockDestinationPos;
    [SerializeField] private float minDropDistance = 2f;
    [SerializeField] private float minPickUpDistance = 5f;
    // [SerializeField] private float progressRewardWeight = 1f;
    [SerializeField] private float dropReward = 1f;
    [SerializeField] private float pickUpReward = 1f;

    [SerializeField] private bool isHoldingTargetBlock;
    [SerializeField] private float distanceFromDestination;
    [SerializeField] private float distanceFromTargetBlock;
    [SerializeField] public Vector2 previousPosition;
    [SerializeField] public Vector2 currentPosition;

    public List<GameObject> spawnedBlocksPerAgent = new List<GameObject>();
    [SerializeField] public GameObject targetBlock;
    [SerializeField] public GameObject destinationObject;
    [SerializeField] public Vector3 targetDirection;

    [SerializeField] private float totalReward_Debug;

    public bool contribute;
    public bool useVectorObs;
    [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
             "is checked, this option has no effect. This option is necessary for the " +
             "VisualFoodCollector scene.")]
    public bool useVectorFrozenFlag;

    EnvironmentParameters m_ResetParams;

    #region Override Functs
    public override void Initialize()
    {   
        //initialize variable values
        previousPosition = Vector2.zero;
        currentPosition = Vector2.zero;

        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
        m_AgentRb = GetComponent<Rigidbody>();
        blockSpawner = area.GetComponent<BlockSpawner>();
        m_EnvironmentSettings = FindObjectOfType<BlockEnvironmentSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        if (!useVectorObs) return;

        var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
        
        // Agent velocity
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        
        // Agent rotation/orientation
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        // Target block relative position (if not holding it)
        if (!isHoldingTargetBlock)
        {
            Vector3 toTargetBlock = (targetBlock.transform.position - transform.position).normalized;
            sensor.AddObservation(toTargetBlock.x);
            sensor.AddObservation(toTargetBlock.z);
        }
        
        // Destination relative position (if holding the block)
        if (isHoldingTargetBlock)
        {
            Vector3 toDestination = (destinationObject.transform.position - transform.position).normalized;
            sensor.AddObservation(toDestination.x);
            sensor.AddObservation(toDestination.z);
        }

        // Target direction for agent to move in to get to block/destination
        sensor.AddObservation(targetDirection);
        
        // Whether the agent is holding the block (binary observation)
        sensor.AddObservation(isHoldingTargetBlock ? 1.0f : 0.0f);
    }


    //User Controlls for agent
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }

        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = Input.GetKey(KeyCode.Alpha7) ? 1 : 0; // pick up block
        discreteActionsOut[1] = Input.GetKey(KeyCode.Alpha8) ? 1 : 0; // drop block in front of agent
    }
    
    //called at beginning of every episode
    //define end condition for episode like 5000 action steps, or calling EndEpisode()
    //define number of actions per epoch like 50000
    public override void OnEpisodeBegin()
    {       
        //update variables for progress reward
        previousPosition = Vector2.zero;
        currentPosition = new Vector2(transform.position.x, transform.position.z);

        m_AgentRb.velocity = Vector3.zero;
        myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        transform.position = new Vector3(Random.Range(-blockSpawner.range, blockSpawner.range),
            2f, Random.Range(-blockSpawner.range, blockSpawner.range))
            + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        SetResetParameters();
        Debug.Log("Episode complete.");

        blockSpawner.ResetBlockArea();
        // this is wrong. Should only reset the individual agents environment, not all the environments
        //m_EnvironmentSettings.EnvironmentReset();
    }

    //called at every action step
    public override void OnActionReceived(ActionBuffers actionBuffers)

    {   
        DropBlock(actionBuffers);
        PickUpBlock(actionBuffers);
        MoveAgent(actionBuffers);

        //update variables for agent observations
        Vector2 agentPosition = new Vector2(transform.position.x, transform.position.z);
        distanceFromDestination = Vector2.Distance(agentPosition, targetBlockDestinationPos);
        distanceFromTargetBlock = Vector2.Distance(agentPosition, targetBlockPos);
        targetBlockPos = new Vector2(targetBlock.transform.position.x, targetBlock.transform.position.z);

        // Calculate rewards
        float matchSpeedReward = GetMatchingVelocityReward(m_AgentRb.velocity);
        float facingReward = GetFacingReward();
        
        // Apply rewards
        AddReward(matchSpeedReward * facingReward);
        totalReward_Debug += matchSpeedReward * facingReward;
    }

    #endregion 

    #region Actions
    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
        }

        else
        {
            myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        }
    }

    // find closest block in the single environment
    GameObject FindClosestBlock()
    {
        List<GameObject> blocks = blockSpawner.spawnedBlocks;

        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject block in blocks)
        {
            if (block == null) 
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, block.transform.position);
            
            // Only consider objects within the minimum distance
            if (distance < closestDistance)
            {
                closest = block;
                closestDistance = distance;
            }
        }
        return closest;
    }
    
    //called in update to keep block object above agent
    void HoldBlockAboveAgent()
    {
        targetBlock.transform.position = transform.position + new Vector3(0, 2, 0);
    }

    //if target block in range, pick it up
    void PickUpBlock(ActionBuffers actionBuffers)
    {
    
        var discreteActions = actionBuffers.DiscreteActions;

        GameObject closestBlock = FindClosestBlock();
        float pickUpDistance = Vector3.Distance(targetBlock.transform.position, closestBlock.transform.position);
        if(discreteActions[0] > 0 && closestBlock == targetBlock && distanceFromTargetBlock <= minPickUpDistance)
        {
            //make agent only able to pick up target block
            isHoldingTargetBlock = true;
            AddReward(pickUpReward);
            totalReward_Debug += pickUpReward;
        }
    }

    //if in range, drop the targetblock onto the destination 
    void DropBlock(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 && isHoldingTargetBlock && (distanceFromDestination <= minDropDistance))
        {
            isHoldingTargetBlock = false;
            targetBlock.transform.position = destinationObject.transform.position + new Vector3(0, 1.5f, 0);

            // need to change this back for inference
            //blockSpawner.SetBlockMaterial(targetBlock, blockSpawner.blockMaterial);
            AddReward(dropReward);
            totalReward_Debug += dropReward;
            EndEpisode();
        }
    }
    #endregion

    #region Reward Functs
    public float GetMatchingVelocityReward(Vector3 actualVelocity)
    {
        float speedGoal = isHoldingTargetBlock ? moveSpeed * 0.8f : moveSpeed; // Adjust speed based on task
        float velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, new Vector3(speedGoal, 0, speedGoal)), 0, moveSpeed);

        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / moveSpeed, 2), 2);
    }

    public float GetFacingReward()
    {
        targetDirection = isHoldingTargetBlock 
            ? (destinationObject.transform.position - transform.position).normalized 
            : (targetBlock.transform.position - transform.position).normalized;

        float alignment = (Vector3.Dot(transform.forward, targetDirection) + 1) * 0.5f; // Normalize from 0 to 1
        return alignment; // Reward scales based on how well-aligned the agent is
    }

    // // if is holding target block, give reward/penalty based on positive or negative progress
    // void ProgressReward()
    // {
    //     float progress = 0;

    //     // If the agent is NOT holding the target block, reward it for moving closer to the block
    //     if (!isHoldingTargetBlock)
    //     {
    //         Vector2 displacement = currentPosition - previousPosition;
    //         Vector2 toBlock = (targetBlockPos - currentPosition).normalized;

    //         // Dot product to measure alignment of movement with "toBlock"
    //         progress = Vector2.Dot(displacement, toBlock);
    //     }
    //     // If the agent IS holding the target block, reward it for moving the block to its destination
    //     else
    //     {
    //         Vector3 displacement = currentPosition - previousPosition;

    //         // Direction from the block to its destination
    //         Vector3 toDestination = new Vector3(
    //             targetBlockDestinationPos.x - targetBlockPos.x,
    //             0f,
    //             targetBlockDestinationPos.y - targetBlockPos.y
    //         ).normalized;

    //         // Dot product for alignment toward destination
    //         progress = Vector3.Dot(displacement, toDestination);
    //     }

    //     AddReward(progress * progressRewardWeight);
    //     totalReward_Debug += progress * progressRewardWeight;
        
    //     // Update positions for the next step
    //     previousPosition = currentPosition;
    //     currentPosition = transform.position;
    // }

    // void FacingReward()
    // {
    //     // Only reward if the agent is actually moving
    //     if (m_AgentRb.velocity.magnitude < 0.1f) return;

    //     float facingReward = 0f;

    //     if (!isHoldingTargetBlock)
    //     {
    //         // Agent should face the target block
    //         Vector3 toTarget = (targetBlock.transform.position - transform.position).normalized;
    //         float alignment = Vector3.Dot(transform.forward, toTarget); // Dot product for facing direction
            
    //         if (alignment > 0.8f) // Close to facing the target block
    //         {
    //             facingReward = 0.1f; // Small positive reward
    //         }
    //     }
    //     else
    //     {
    //         // Agent should face the destination
    //         Vector3 toDestination = (destinationObject.transform.position - transform.position).normalized;
    //         float alignment = Vector3.Dot(transform.forward, toDestination);

    //         if (alignment > 0.8f) // Close to facing the destination
    //         {
    //             facingReward = 0.1f; // Small positive reward
    //         }
    //     }

    //     AddReward(facingReward);
    //     totalReward_Debug += facingReward;
    // }
    #endregion

    #region Other
    void Update()
    {
        if(isHoldingTargetBlock)
        {
            HoldBlockAboveAgent();
        }
    }
    public void SetLaserLengths()
    {
        m_LaserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void SetResetParameters()
    {
        SetLaserLengths();
        SetAgentScale();
    }
    #endregion
}
