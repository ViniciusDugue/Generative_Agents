// using UnityEngine;
// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;
// using UnityEngine.AI;
// using Random = UnityEngine.Random;
// using System.Collections.Generic;

// public class BlockAgent3 : Agent
// {
//     public BlockEnvironmentSettings m_EnvironmentSettings;
//     public GameObject area;
//     [SerializeField] public BlockSpawner blockSpawner;
    
//     // NavMeshAgent for navigation
//     private NavMeshAgent navAgent;
    
//     public Material normalMaterial;
//     public GameObject myLaser;
    
//     // Movement parameters
//     public float moveSpeed = 2f;
//     public float turnSpeed = 300f; // degrees per second
    
//     [SerializeField] public Vector3 targetBlockPos;
//     [SerializeField] public Vector3 targetBlockDestinationPos;
//     [SerializeField] private float minDropDistance = 2f;
//     [SerializeField] private float minPickUpDistance = 5f;
//     [SerializeField] private float dropReward = 1f;
//     [SerializeField] private float pickUpReward = 1f;
    
//     [SerializeField] private bool isHoldingTargetBlock;
//     [SerializeField] private float distanceFromDestination;
//     [SerializeField] private float distanceFromTargetBlock;
//     [SerializeField] public Vector3 previousPosition;
//     [SerializeField] public Vector3 currentPosition;
    
//     public List<GameObject> spawnedBlocksPerAgent = new List<GameObject>();
//     [SerializeField] public GameObject targetBlock;
//     [SerializeField] public GameObject destinationObject;
//     [SerializeField] public Vector3 targetDirection;
    
//     [SerializeField] private float progressReward;
//     [SerializeField] private float progressReward_Debug;
//     [SerializeField] private float pickUpReward_Debug;
//     [SerializeField] private float dropReward_Debug;
//     [SerializeField] private float totalReward_Debug;
//     [SerializeField] private float alignment;
//     [SerializeField] private float alignmentAngleThreshold = 30f;
    
//     public bool contribute;
//     public bool useVectorObs;
//     public bool useVectorFrozenFlag;
    
//     EnvironmentParameters m_ResetParams;
    
//     #region Override Functions
//     public override void Initialize()
//     {
//         previousPosition = Vector3.zero;
//         currentPosition = Vector3.zero;
        
//         gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
        
//         navAgent = GetComponent<NavMeshAgent>();
//         if (navAgent == null)
//         {
//             Debug.LogError("NavMeshAgent component not found!");
//         }
//         // Disable automatic rotation so we can control it manually.
//         navAgent.updateRotation = false;
        
//         blockSpawner = area.GetComponent<BlockSpawner>();
//         m_EnvironmentSettings = FindObjectOfType<BlockEnvironmentSettings>();
//         m_ResetParams = Academy.Instance.EnvironmentParameters;
//         SetResetParameters();
//     }
    
//     public override void CollectObservations(VectorSensor sensor)
//     {
//         if (!useVectorObs) return;
        
//         // nav mesh velocity
//         var localVelocity = transform.InverseTransformDirection(navAgent.velocity);
//         sensor.AddObservation(localVelocity);
        
//         //  the agent's rotation
//         sensor.AddObservation(transform.forward);
        
//         // target direction:
//         // If not holding a block, the direction to the target block; if holding, the direction to the destination.
//         if (!isHoldingTargetBlock)
//         {
//             Vector3 toTargetBlock = (targetBlock.transform.position - transform.position).normalized;
//             sensor.AddObservation(toTargetBlock);
//         }
//         else
//         {
//             Vector3 toDestination = (destinationObject.transform.position - transform.position).normalized;
//             sensor.AddObservation(toDestination);
//         }
        
//         // the manually controlled target direction (for reward shaping)
//         sensor.AddObservation(targetDirection);
        
//         // if the agent is holding the block (binary)
//         sensor.AddObservation(isHoldingTargetBlock ? 1f : 0f);
        
//         // alignment between the agent's forward direction and the target direction
//         sensor.AddObservation(alignment);
        
//         sensor.AddObservation(distanceFromDestination);
//         sensor.AddObservation(distanceFromTargetBlock);
//     }
    
//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var continuousActionsOut = actionsOut.ContinuousActions;
//         // W/S controls forward/backward; A/D controls rotation.
//         continuousActionsOut[0] = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);
//         continuousActionsOut[1] = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        
//         var discreteActionsOut = actionsOut.DiscreteActions;
//         // Key 7 to pick up block, Key 8 to drop block.
//         discreteActionsOut[0] = Input.GetKey(KeyCode.Alpha7) ? 1 : 0;
//         discreteActionsOut[1] = Input.GetKey(KeyCode.Alpha8) ? 1 : 0;
//     }
    
//     public override void OnEpisodeBegin()
//     {
//         navAgent.ResetPath();
//         previousPosition = Vector3.zero;
//         currentPosition = transform.position;
        
//         // Randomly reposition and orient the agent.
//         transform.position = new Vector3(Random.Range(-blockSpawner.range, blockSpawner.range),
//                                          2f,
//                                          Random.Range(-blockSpawner.range, blockSpawner.range))
//                                          + area.transform.position;
//         transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
//         SetResetParameters();
        
//         Debug.Log("Episode complete.");
//         totalReward_Debug = 0;
//         pickUpReward_Debug = 0;
//         dropReward_Debug = 0;
//         progressReward_Debug = 0;
//         blockSpawner.ResetBlockArea();
//     }
    
//     public override void OnActionReceived(ActionBuffers actionBuffers)
//     {
//         MoveAgent(actionBuffers);
//         DropBlock(actionBuffers);
//         PickUpBlock(actionBuffers);
        
//         // Update observation variables.
//         Vector3 agentPosition = transform.position;
//         targetBlockDestinationPos = destinationObject.transform.position;
//         distanceFromDestination = Vector3.Distance(agentPosition, targetBlockDestinationPos);
//         distanceFromTargetBlock = Vector3.Distance(agentPosition, targetBlock.transform.position);
//         targetBlockPos = targetBlock.transform.position;
        
//         CalculateProgressReward();
//     }
//     #endregion
    
//     #region Movement
//     public void MoveAgent(ActionBuffers actionBuffers)
//     {
//         var continuousActions = actionBuffers.ContinuousActions;
//         // forwardInput controls forward/backward movement.
//         float forwardInput = Mathf.Clamp(continuousActions[0], -1f, 1f);
//         // rotationInput controls left/right rotation.
//         float rotationInput = Mathf.Clamp(continuousActions[1], -1f, 1f);
        
//         // If there is rotation input, cancel any previous movement so momentum doesn't carry over.
//         if (Mathf.Abs(rotationInput) > 0.01f)
//         {
//             navAgent.SetDestination(transform.position);
//         }
        
//         // Rotate the agent manually.
//         float rotationAmount = rotationInput * turnSpeed * Time.fixedDeltaTime;
//         transform.Rotate(0, rotationAmount, 0);
        
//         // After rotation, update movement if there is forward input.
//         if (Mathf.Abs(forwardInput) > 0.01f)
//         {
//             // Always use the current forward direction after rotation.
//             Vector3 targetPosition = transform.position + transform.forward * forwardInput * moveSpeed;
//             navAgent.SetDestination(targetPosition);
//             targetDirection = transform.forward;
//         }
//         else
//         {
//             // Stop moving if no forward input.
//             navAgent.SetDestination(transform.position);
//             targetDirection = Vector3.zero;
//         }
//     }
//     #endregion
    
//     #region Block Actions
//     GameObject FindClosestBlock()
//     {
//         List<GameObject> blocks = blockSpawner.spawnedBlocks;
//         GameObject closest = null;
//         float closestDistance = Mathf.Infinity;
//         foreach (GameObject block in blocks)
//         {
//             if (block == null) continue;
//             float distance = Vector3.Distance(transform.position, block.transform.position);
//             if (distance < closestDistance)
//             {
//                 closest = block;
//                 closestDistance = distance;
//             }
//         }
//         return closest;
//     }
    
//     void HoldBlockAboveAgent()
//     {
//         targetBlock.transform.position = transform.position + new Vector3(0, 2, 0);
//     }
    
//     void PickUpBlock(ActionBuffers actionBuffers)
//     {
//         var discreteActions = actionBuffers.DiscreteActions;
//         GameObject closestBlock = FindClosestBlock();
//         float pickUpDistance = Vector3.Distance(targetBlock.transform.position, closestBlock.transform.position);
//         if (discreteActions[0] > 0 && closestBlock == targetBlock && distanceFromTargetBlock <= minPickUpDistance && !isHoldingTargetBlock)
//         {
//             isHoldingTargetBlock = true;
//             AddReward(pickUpReward);
//             pickUpReward_Debug += pickUpReward;
//             totalReward_Debug += pickUpReward;
//         }
        
//     }
    
//     void DropBlock(ActionBuffers actionBuffers)
//     {
//         var discreteActions = actionBuffers.DiscreteActions;
//         if (discreteActions[1] > 0 && isHoldingTargetBlock && (distanceFromDestination <= minDropDistance))
//         {
//             isHoldingTargetBlock = false;
//             targetBlock.transform.position = destinationObject.transform.position + new Vector3(0, 1.5f, 0);
//             AddReward(dropReward);
//             dropReward_Debug += dropReward;
//             totalReward_Debug += dropReward;
//             EndEpisode();
//         }
//     }
//     #endregion
    
//     #region Reward Functions
//     public float CalculateFacingReward()
//     {
//         targetDirection = isHoldingTargetBlock
//             ? (destinationObject.transform.position - transform.position).normalized
//             : (targetBlock.transform.position - transform.position).normalized;
//         float dotProduct = Vector3.Dot(transform.forward, targetDirection);
//         float angle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * Mathf.Rad2Deg;
//         alignment = (angle <= alignmentAngleThreshold) ? dotProduct : 0f;
//         return alignment;
//     }
    
//     public void CalculateProgressReward()
//     {
//         float alignmentVal = CalculateFacingReward();
//         float forwardVelocity = Vector3.Dot(navAgent.velocity.normalized, transform.forward);
//         float progressFactor = Mathf.Clamp(forwardVelocity, 0, 1);
//         progressReward = (navAgent.velocity.magnitude * progressFactor * alignmentVal) * 0.01f;
//         progressReward_Debug += progressReward;
//     }
//     #endregion
    
//     #region Other
//     void Update()
//     {
//         if (isHoldingTargetBlock)
//         {
//             HoldBlockAboveAgent();
//         }
//     }
    
//     public void SetLaserLengths()
//     {
//         float laserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
//     }
    
//     public void SetAgentScale()
//     {
//         float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
//         transform.localScale = new Vector3(agentScale, agentScale, agentScale);
//     }
    
//     public void SetResetParameters()
//     {
//         SetLaserLengths();
//         SetAgentScale();
//     }
//     #endregion
// }