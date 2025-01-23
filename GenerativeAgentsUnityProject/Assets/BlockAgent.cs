using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockAgent : MonoBehaviour
{
    public EnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    FoodSpawner m_MyArea;
    bool m_Shoot;

    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public Material normalMaterial;
    public Material badMaterial;
    public Material goodMaterial;
    public Material frozenMaterial;
    public GameObject myLaser;
    public bool contribute;
    public bool useVectorObs;
    [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
             "is checked, this option has no effect. This option is necessary for the " +
             "VisualFoodCollector scene.")]
    public bool useVectorFrozenFlag;

    [SerializeField] public Vector2 currentAgentPos;
    [SerializeField] public Vector2 targetBlockCurrentPos;
    [SerializeField] public Vector2 targetBlockDestinationPos;
    [SerializeField] public GameObject closestBlock;
    [SerializeField] public bool HoldingBlock;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<FoodSpawner>();
        m_EnvironmentSettings = FindObjectOfType<EnvironmentSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();

        targetBlockCurrentPos = ;
        targetBlockDestinationPos = ;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
            sensor.AddObservation(m_Frozen);
            sensor.AddObservation(m_Shoot);

            //rotation observation
            sensor.AddObservation(localRotation);

            //block current position
            sensor.AddObservation(targetBlockCurrentPos);

            //block destination
            sensor.AddObservation(targetBlockDestinationPos);

        }
        else if (useVectorFrozenFlag)
        {
            sensor.AddObservation(m_Frozen);
        }
    }

    void Update()
    {
        // lets say the destination is 5 units away from the block
        if(HoldingBlock && IsProgressMade())
        {
            AddReward(0.0001f);
        }
        
    }

    bool IsProgressMade()
    {
        Vector3 currentPosition = transform.position;
        Vector3 toDestination = destination - currentPosition;

        if (toDestination.magnitude > 0.01f)
        {
            Vector3 direction = toDestination.normalized;
            float progress = Vector3.Dot(velocity, direction);
        }        
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        m_Shoot = false;

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

        //slow down agent so velocity doesnt increase endlessly
        if (m_AgentRb.velocity.sqrMagnitude > 25f) 
        {
            m_AgentRb.velocity *= 0.95f;
        }

    }

    void PickUpBlock()
    {
        //find closest block object and call its function;
    
        var discreteActions = actionBuffers.DiscreteActions;

        if(discreteActions[1] > 0 )
        {
            closestBlock = FindClosestBlock();
            closestBlock.GetComponent<BlockScript>().PickUpBlock();
        }
    }

    void DropBlock()
    {
        //drop the picked up block
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
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;

        discreteActionsOut[1] = Input.GetKey(KeyCode.Alpha7) ? 1 : 0; // pick up block
        discreteActionsOut[2] = Input.GetKey(KeyCode.Alpha8) ? 1 : 0; // drop block in front of agent
    }

    public override void OnEpisodeBegin()
    {       
        Unfreeze();
        Unpoison();
        Unsatiate();
        m_Shoot = false;
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
