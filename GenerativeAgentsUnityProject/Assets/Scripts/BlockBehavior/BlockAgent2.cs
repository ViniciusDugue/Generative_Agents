// using UnityEngine;
// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;
// using Random = UnityEngine.Random;
// using System.Collections.Generic; 

// public class BlockAgent2 : Agent
// {
//     public BlockEnvironmentSettings m_EnvironmentSettings;
//     public GameObject area;
//     [SerializeField] public BlockSpawner blockSpawner;
//     Rigidbody m_AgentRb;
//     float m_LaserLength;
//     // Speed of agent rotation.
//     public float turnSpeed = 300;

//     // Speed of agent movement.
//     public float moveSpeed = 2;
//     public Material normalMaterial;
//     public GameObject myLaser;

//     [SerializeField] public Vector3 targetBlockPos;
//     [SerializeField] public Vector3 targetBlockDestinationPos;
//     [SerializeField] private float minDropDistance = 2f;
//     [SerializeField] private float minPickUpDistance = 5f;
//     // [SerializeField] private float progressRewardWeight = 1f;
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
//     [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
//              "is checked, this option has no effect. This option is necessary for the " +
//              "VisualFoodCollector scene.")]
//     public bool useVectorFrozenFlag;

//     EnvironmentParameters m_ResetParams;

//     #region Override Functs
//     public override void Initialize()
//     {   
//         //initialize variable values
//         previousPosition = Vector3.zero;
//         currentPosition = Vector3.zero;

//         gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
//         m_AgentRb = GetComponent<Rigidbody>();
//         blockSpawner = area.GetComponent<BlockSpawner>();
//         m_EnvironmentSettings = FindObjectOfType<BlockEnvironmentSettings>();
//         m_ResetParams = Academy.Instance.EnvironmentParameters;
//         SetResetParameters();
//     }
    
//     public override void CollectObservations(VectorSensor sensor)
//     {
//         if (!useVectorObs) return;

//         var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
        
//         // Agent velocity
//         sensor.AddObservation(localVelocity);
        
//         // Agent rotation/orientation
//         sensor.AddObservation(transform.forward);

//         // Target block relative position (if not holding it)
//         if (!isHoldingTargetBlock)
//         {
//             Vector3 toTargetBlock = (targetBlock.transform.position - transform.position).normalized;
//             sensor.AddObservation(toTargetBlock);
//         }
        
//         // Destination relative position (if holding the block)
//         if (isHoldingTargetBlock)
//         {
//             Vector3 toDestination = (destinationObject.transform.position - transform.position).normalized;
//             sensor.AddObservation(toDestination);
//         }

//         // Target direction for agent to move in to get to block/destination
//         sensor.AddObservation(targetDirection);
        
//         // Whether the agent is holding the block (binary observation)
//         sensor.AddObservation(isHoldingTargetBlock ? 1.0f : 0.0f);

//         // how aligned the agent is towards the object
//         sensor.AddObservation(alignment);

//         sensor.AddObservation(distanceFromDestination);
//         sensor.AddObservation(distanceFromTargetBlock);
//     }


//     //User Controlls for agent
//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var continuousActionsOut = actionsOut.ContinuousActions;
//         if (Input.GetKey(KeyCode.A))
//         {
//             continuousActionsOut[2] = 1;
//         }
//         if (Input.GetKey(KeyCode.W))
//         {
//             continuousActionsOut[0] = 1;
//         }
//         if (Input.GetKey(KeyCode.D))
//         {
//             continuousActionsOut[2] = -1;
//         }
//         if (Input.GetKey(KeyCode.S))
//         {
//             continuousActionsOut[0] = -1;
//         }

//         var discreteActionsOut = actionsOut.DiscreteActions;

//         discreteActionsOut[0] = Input.GetKey(KeyCode.Alpha7) ? 1 : 0; // pick up block
//         discreteActionsOut[1] = Input.GetKey(KeyCode.Alpha8) ? 1 : 0; // drop block in front of agent
//     }
    
//     //called at beginning of every episode
//     //define end condition for episode like 5000 action steps, or calling EndEpisode()
//     //define number of actions per epoch like 50000
//     public override void OnEpisodeBegin()
//     {       
//         //update variables for progress reward
//         previousPosition = Vector3.zero;
//         currentPosition = transform.position;

//         m_AgentRb.velocity = Vector3.zero;
//         myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
//         transform.position = new Vector3(Random.Range(-blockSpawner.range, blockSpawner.range),
//             2f, Random.Range(-blockSpawner.range, blockSpawner.range))
//             + area.transform.position;
//         transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

//         SetResetParameters();
//         Debug.Log("Episode complete.");
//         totalReward_Debug = 0;
//         pickUpReward_Debug = 0;
//         dropReward_Debug = 0;
//         progressReward_Debug = 0;
//         blockSpawner.ResetBlockArea();
//         // this is wrong. Should only reset the individual agents environment, not all the environments
//         //m_EnvironmentSettings.EnvironmentReset();
//     }

//     //called at every action step
//     public override void OnActionReceived(ActionBuffers actionBuffers)

//     {   
//         DropBlock(actionBuffers);
//         PickUpBlock(actionBuffers);
//         MoveAgent(actionBuffers);

//         //update variables for agent observations
//         Vector3 agentPosition = transform.position;
//         targetBlockDestinationPos = destinationObject.transform.position;
//         distanceFromDestination = Vector3.Distance(agentPosition, targetBlockDestinationPos);
//         distanceFromTargetBlock = Vector3.Distance(agentPosition, targetBlockPos);
//         targetBlockPos = targetBlock.transform.position;

//         CalculateProgressReward();
//     }

//     #endregion 

//     #region Actions
//     public void MoveAgent(ActionBuffers actionBuffers)
//     {
//         var dirToGo = Vector3.zero;
//         var rotateDir = Vector3.zero;

//         var continuousActions = actionBuffers.ContinuousActions;
//         var discreteActions = actionBuffers.DiscreteActions;

//         var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
//         var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
//         var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

//         dirToGo = transform.forward * forward;
//         dirToGo += transform.right * right;
//         rotateDir = -transform.up * rotate;

//         m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
//         transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

//         if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
//         {
//             m_AgentRb.velocity *= 0.95f;
//         }

//         else
//         {
//             myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
//         }
//     }

//     // find closest block in the single environment
//     GameObject FindClosestBlock()
//     {
//         List<GameObject> blocks = blockSpawner.spawnedBlocks;

//         GameObject closest = null;
//         float closestDistance = Mathf.Infinity;

//         foreach (GameObject block in blocks)
//         {
//             if (block == null) 
//             {
//                 continue;
//             }

//             float distance = Vector3.Distance(transform.position, block.transform.position);
            
//             // Only consider objects within the minimum distance
//             if (distance < closestDistance)
//             {
//                 closest = block;
//                 closestDistance = distance;
//             }
//         }
//         return closest;
//     }
    
//     //called in update to keep block object above agent
//     void HoldBlockAboveAgent()
//     {
//         targetBlock.transform.position = transform.position + new Vector3(0, 2, 0);
//     }

//     //if target block in range, pick it up
//     void PickUpBlock(ActionBuffers actionBuffers)
//     {
    
//         var discreteActions = actionBuffers.DiscreteActions;

//         GameObject closestBlock = FindClosestBlock();
//         float pickUpDistance = Vector3.Distance(targetBlock.transform.position, closestBlock.transform.position);

//         if(discreteActions[0] > 0 && closestBlock == targetBlock && distanceFromTargetBlock <= minPickUpDistance && !isHoldingTargetBlock)
//         {
//             //make agent only able to pick up target block
//             isHoldingTargetBlock = true;
//             AddReward(pickUpReward);
//             pickUpReward_Debug +=pickUpReward;
//             totalReward_Debug += pickUpReward;
//         }
//     }

//     //if in range, drop the targetblock onto the destination 
//     void DropBlock(ActionBuffers actionBuffers)
//     {
//         var discreteActions = actionBuffers.DiscreteActions;

//         if(discreteActions[1] > 0 && isHoldingTargetBlock && (distanceFromDestination <= minDropDistance))
//         {
//             isHoldingTargetBlock = false;
//             targetBlock.transform.position = destinationObject.transform.position + new Vector3(0, 1.5f, 0);

//             // need to change this back for inference
//             //blockSpawner.SetBlockMaterial(targetBlock, blockSpawner.blockMaterial);
//             AddReward(dropReward);
//             dropReward_Debug += dropReward;
//             totalReward_Debug += dropReward;
//             EndEpisode();
//         }
//     }
//     #endregion

//     #region Reward Functs
//     //calculates alignment of agent towards target directions(towards target block and destination)
//     // linear alignment
//     // public float CalculateFacingReward()
//     // {
//     //     targetDirection = isHoldingTargetBlock 
//     //         ? (destinationObject.transform.position - transform.position).normalized 
//     //         : (targetBlock.transform.position - transform.position).normalized;

//     //     // normalized to [-1, 1] range
//     //     alignment = Vector3.Dot(transform.forward, targetDirection); 
//     //     return alignment;
//     // }

//     //linear alignment with degree cone threshold. If outside the angle threshold from target and is positive, then set it to 0
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

//     // logarithmic alignment(penalizes going in circles and also not facing towards target direction even more)
//     // public float CalculateFacingReward()
//     // {
//     //     targetDirection = isHoldingTargetBlock
//     //         ? (destinationObject.transform.position - transform.position).normalized
//     //         : (targetBlock.transform.position - transform.position).normalized;

//     //     // Calculate alignment (ranges from -1 to 1)
//     //     float rawAlignment = Vector3.Dot(transform.forward, targetDirection); 

//     //     // Logarithmic transformation for both reward (positive) and penalty (negative)
//     //     float logAlignment;
//     //     if (rawAlignment > 0)
//     //     {
//     //         logAlignment = Mathf.Log10(1 + (rawAlignment * 9));
//     //     }
//     //     else
//     //     {
//     //         logAlignment = -Mathf.Log10(1 + (-rawAlignment * 9));
//     //     }

//     //     alignment = logAlignment;
//     //     return logAlignment;
//     // }

//     //Rewards agent for moving towards target areas and Penalizes moving away
//     public void CalculateProgressReward()
//     {
//         float alignment = CalculateFacingReward(); 

//         float forwardVelocity = Vector3.Dot(m_AgentRb.velocity.normalized, transform.forward);
//         float progressFactor = Mathf.Clamp(forwardVelocity, 0, 1);

//         progressReward = (m_AgentRb.velocity.magnitude * progressFactor * alignment) * 0.01f;
//         progressReward_Debug += progressReward;
//         //AddReward(progressReward);

//         //totalReward_Debug += progressReward;
//     }


    

//     #endregion

//     #region Other
//     void Update()
//     {
//         if(isHoldingTargetBlock)
//         {
//             HoldBlockAboveAgent();
//         }
//     }
//     public void SetLaserLengths()
//     {
//         m_LaserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
//     }

//     public void SetAgentScale()
//     {
//         float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
//         gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
//     }

//     public void SetResetParameters()
//     {
//         SetLaserLengths();
//         SetAgentScale();
//     }
//     #endregion
// }