using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BlockAgent : Agent
{
    public BlockEnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    BlockSpawner m_MyArea;

    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public GameObject myLaser;
    public bool contribute;
    public bool useVectorObs;
    public bool useVectorFrozenFlag;

    [SerializeField] public Vector2 currentAgentPos;
    [SerializeField] public Vector2 targetBlockCurrentPos;
    [SerializeField] public Vector2 targetBlockDestinationPos;
    [SerializeField] public GameObject closestBlock;
    [SerializeField] public bool IsHoldingBlock;
    [SerializeField] private Vector3 previousPosition;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<BlockSpawner>();
        m_EnvironmentSettings = FindObjectOfType<BlockEnvironmentSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();

        targetBlockCurrentPos = Vector2.zero;
        targetBlockDestinationPos = Vector2.zero;
        previousPosition = transform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);

            //rotation observation
            sensor.AddObservation(transform.localRotation.x);
            sensor.AddObservation(transform.localRotation.z);

            //block current position
            sensor.AddObservation(targetBlockCurrentPos.x);
            sensor.AddObservation(targetBlockCurrentPos.y);

            //block destination
            sensor.AddObservation(targetBlockDestinationPos.x);
            sensor.AddObservation(targetBlockDestinationPos.y);

        }
    }

    float CalculateProgressTowardsDestination()
    {
        if (targetBlockDestinationPos == Vector2.zero || targetBlockCurrentPos == Vector2.zero)
            return 0f;

        Vector3 currentPosition = transform.position;
        Vector3 displacement = currentPosition - previousPosition;

        Vector3 toDestination = new Vector3(
            targetBlockDestinationPos.x - targetBlockCurrentPos.x,
            0f,
            targetBlockDestinationPos.y - targetBlockCurrentPos.y
        ).normalized;

        float progress = Vector3.Dot(displacement, toDestination);

        return progress;
    }

    float CalculateFacingReward()
    {
        if (targetBlockDestinationPos == Vector2.zero)
            return 0f;

        Vector3 toDestination = new Vector3(
            targetBlockDestinationPos.x - transform.position.x,
            0f,
            targetBlockDestinationPos.y - transform.position.z
        ).normalized;

        float alignment = Vector3.Dot(transform.forward, toDestination);

        return Mathf.Clamp(alignment, 0f, 1f); 
    }

    void Update()
    {
        // lets say the destination is 5 units away from the block
        float progress = CalculateProgressTowardsDestination();

        if(IsHoldingBlock)
        {
            AddReward(progress * 0.1f);
        }

        //reward for facing in right direction
        float facingReward = CalculateFacingReward();
        AddReward(facingReward * 0.1f);

        previousPosition = transform.position;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("block"))
        {
            if (collision.gameObject != closestBlock)
            {
                AddReward(-0.01f);
            }
        }
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        //movement 
        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        //honestly idk what thius is for?
        if(IsHoldingBlock)
        {
            dirToGo *= 0.5f;
            m_AgentRb.velocity *= 0.75f;
        }
        //slow down agent so velocity doesnt increase endlessly
        if (m_AgentRb.velocity.sqrMagnitude > 25f) 
        {
            m_AgentRb.velocity *= 0.95f;
        }

    }

    void PickUpBlock(ActionBuffers actionBuffers)
    {
        //find closest block object and call its function;
    
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 )
        {
            closestBlock = FindClosestBlock();
            closestBlock.GetComponent<BlockScript>().PickUpBlock();
        }
    }

    void DropBlock(ActionBuffers actionBuffers)
    {
        //drop the picked up block

        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 )
        {
            closestBlock.GetComponent<BlockScript>().DropBlock();
        }
    }

    GameObject FindClosestBlock()
    {
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("block");

        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject obj in objectsWithTag)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < closestDistance)
            {
                closest = obj;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.A))
        {
            Debug.Log("Turn Left");
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            Debug.Log("Move Forward");
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Debug.Log("Turn Right");
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Debug.Log("Move Backward");
            continuousActionsOut[0] = -1;
        }

        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;

        discreteActionsOut[1] = Input.GetKey(KeyCode.Alpha7) ? 1 : 0; // pick up block
        discreteActionsOut[2] = Input.GetKey(KeyCode.Alpha8) ? 1 : 0; // drop block in front of agent
    }

    public override void OnEpisodeBegin()
    {
        m_AgentRb.velocity = Vector3.zero;

        transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range), 1f, Random.Range(-m_MyArea.range, m_MyArea.range));
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 randomPosition = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range), 0f, Random.Range(-m_MyArea.range, m_MyArea.range));
        targetBlockCurrentPos = new Vector2(randomPosition.x, randomPosition.z);

        Vector3 destinationPosition = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range), 0f, Random.Range(-m_MyArea.range, m_MyArea.range));
        targetBlockDestinationPos = new Vector2(destinationPosition.x, destinationPosition.z);

        Debug.Log("Episode started. Block positions updated.");
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
}
