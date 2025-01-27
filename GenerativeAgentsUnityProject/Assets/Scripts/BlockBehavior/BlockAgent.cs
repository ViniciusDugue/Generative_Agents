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

    [SerializeField] public GameObject pickedUpBlock;
    [SerializeField] public bool isHoldingBlock;
    [SerializeField] public Vector2 targetBlockPos;
    [SerializeField] public Vector2 targetBlockDestinationPos;
    [SerializeField] private float minimumDistance = 5.0f;
    [SerializeField] private float progressRewardWeight = 1f;
    [SerializeField] private bool isHoldingTargetBlock;
    [SerializeField] private float distanceFromDestination;
    [SerializeField] private float distanceFromTargetBlock;
    [SerializeField] public Vector2 previousPosition;
    [SerializeField] public Vector2 currentPosition;

    public List<GameObject> spawnedBlocksPerAgent = new List<GameObject>();
    [SerializeField] public GameObject targetBlock;
    [SerializeField] public GameObject destinationObject;

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
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            //agent velocity
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);

            //agent rotation/orientation
            sensor.AddObservation(transform.localRotation.x);
            sensor.AddObservation(transform.localRotation.y);

            //agent position
            sensor.AddObservation(transform.position.x);
            sensor.AddObservation(transform.position.y);

            //target block position
            sensor.AddObservation(targetBlockPos.x);
            sensor.AddObservation(targetBlockPos.y);
            
            //block destination
            sensor.AddObservation(targetBlockDestinationPos.x);
            sensor.AddObservation(targetBlockDestinationPos.y);
            
            //distance from destination(variable needs to be updated in agent script)
            sensor.AddObservation(distanceFromDestination);

            //distance from targetBlock(variable needs to be updated in agent script)
            sensor.AddObservation(distanceFromTargetBlock);

            //whether agent is holding block
            sensor.AddObservation(isHoldingBlock);

            //whether agent is holding target block
            sensor.AddObservation(isHoldingTargetBlock);
            
        }
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
        m_EnvironmentSettings.EnvironmentReset();
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

        ProgressReward();
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

        // var shootCommand = discreteActions[0] > 0;
        // if (shootCommand)
        // {
        //     m_Shoot = true;
        //     dirToGo *= 0.5f;
        //     m_AgentRb.velocity *= 0.75f;
        // }
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

    // find closest block object in the single environment
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
            if (distance < closestDistance && distance <= minimumDistance)
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
        pickedUpBlock.transform.position = transform.position + new Vector3(0, 2, 0);
    }

    //find closest block object and pick it up
    void PickUpBlock(ActionBuffers actionBuffers)
    {
    
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[0] > 0 )
        {
            pickedUpBlock = FindClosestBlock();

            isHoldingBlock = true;

            //this might be bad incase another agent accidentally picks up another agents target block
            //should be fine because targetblockposition is updated
            isHoldingTargetBlock = pickedUpBlock == targetBlock ? true : false;
            if(isHoldingTargetBlock)
            {
                AddReward(5f);
            }
        }
    }

    //drop the picked up block in front of agent
    void DropBlock(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 && isHoldingBlock)
        {
            isHoldingBlock = false;
            pickedUpBlock.transform.position = transform.position + transform.forward + new Vector3(0, 1.5f, 0);
        }
    }
    #endregion

    #region Reward Functs
    // if is holding target block, give reward/penalty based on positive or negative progress
    void ProgressReward()
    {   
        float progress = 0;
        
        if(isHoldingTargetBlock)
        {
            Vector3 displacement = currentPosition - previousPosition;

            Vector3 toDestination = new Vector3(
                targetBlockDestinationPos.x - targetBlockPos.x,
                0f,
                targetBlockDestinationPos.y - targetBlockPos.y
            ).normalized;

            progress = Vector3.Dot(displacement, toDestination);
        }
        //Debug.Log($"Progress Reward: {progress}");
        AddReward(progress * progressRewardWeight);

        //update variables for next action step
        previousPosition = currentPosition;
        currentPosition = transform.position;
    }

    //when agent collides with destination trigger, do drop block reward
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("destination") && isHoldingTargetBlock)
        {
            //set block color back to default color
            blockSpawner.SetBlockMaterial(targetBlock, blockSpawner.blockMaterial);
            AddReward(5f);
        }
    }
    #endregion

    #region Other
    void Update()
    {
        if(isHoldingBlock)
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
