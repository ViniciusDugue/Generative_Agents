using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BlockAgent2 : Agent
{
    public EnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    FoodSpawner m_MyArea;
    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public Material normalMaterial;
    public GameObject myLaser;
    public bool contribute;
    public bool useVectorObs;
    [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
             "is checked, this option has no effect. This option is necessary for the " +
             "VisualFoodCollector scene.")]
    public bool useVectorFrozenFlag;

    EnvironmentParameters m_ResetParams;

    [SerializeField] public GameObject closestBlock;
    [SerializeField] public bool IsHoldingBlock;
    [SerializeField] public Vector2 targetBlockCurrentPos;
    [SerializeField] public Vector2 targetBlockDestinationPos;
    [SerializeField] private float minimumDistance = 5.0f;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<FoodSpawner>();
        m_EnvironmentSettings = FindObjectOfType<EnvironmentSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);

            sensor.AddObservation(transform.localRotation.x);
            sensor.AddObservation(transform.localRotation.z);

            sensor.AddObservation(targetBlockCurrentPos.x);
            sensor.AddObservation(targetBlockCurrentPos.y);
            
            //block destination
            sensor.AddObservation(targetBlockDestinationPos.x);
            sensor.AddObservation(targetBlockDestinationPos.y);

            sensor.AddObservation(IsHoldingBlock);
        }
    }
    
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

    GameObject FindClosestBlock()
    {
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("block");

        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject obj in objectsWithTag)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            
            // Only consider objects within the minimum distance
            if (distance < closestDistance && distance <= minimumDistance)
            {
                closest = obj;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void Update()
    {
        if(IsHoldingBlock)
        {
            HoldBlockAboveAgent();
        }
    }

    void HoldBlockAboveAgent()
    {
        closestBlock.transform.position = transform.position + new Vector3(0, 2, 0);
    }

    void PickUpBlock(ActionBuffers actionBuffers)
    {
        //find closest block object and pick it up
    
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[0] > 0 )
        {
            closestBlock = FindClosestBlock();

            IsHoldingBlock = true;
        }
    }

    void DropBlock(ActionBuffers actionBuffers)
    {
        //drop the picked up block

        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 && IsHoldingBlock)
        {
            IsHoldingBlock = false;
            closestBlock.transform.position = transform.position + transform.forward + new Vector3(0, 1.5f, 0);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        DropBlock(actionBuffers);

        PickUpBlock(actionBuffers);
        MoveAgent(actionBuffers);
    }

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

    public override void OnEpisodeBegin()
    {       
        m_AgentRb.velocity = Vector3.zero;
        myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
            2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        SetResetParameters();
        Debug.Log("Episode complete.");
        m_EnvironmentSettings.EnvironmentReset();
    }

    void OnCollisionEnter(Collision collision)
    {

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
