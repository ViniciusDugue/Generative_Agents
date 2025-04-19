using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class FoodGathererAgent : AgentBehavior
{
    public EnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    // FoodSpawner m_MyArea;
    bool m_Frozen;
    bool m_Poisoned;
    bool m_Satiated;
    bool m_Shoot;
    float m_FrozenTime;
    float m_EffectTime;
    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public bool agentReset = true;
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
    

    EnvironmentParameters m_ResetParams;

    //initialize necessary parameters
    public override void Initialize()
    {
        // exhaustionRate = 2.0f;
        m_AgentRb = GetComponent<Rigidbody>();
        m_EnvironmentSettings = FindObjectOfType<EnvironmentSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    //collect observations from environment to feed as input for agent model
    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
            sensor.AddObservation(m_Frozen);
            sensor.AddObservation(m_Shoot);
        }
        else if (useVectorFrozenFlag)
        {
            sensor.AddObservation(m_Frozen);
        }
    }

    //changes agent color
    public Color32 ToColor(int hexVal)
    {
        var r = (byte)((hexVal >> 16) & 0xFF);
        var g = (byte)((hexVal >> 8) & 0xFF);
        var b = (byte)(hexVal & 0xFF);
        return new Color32(r, g, b, 255);
    }

    //agent model outputs/action buffers are turned into actions onto the environment
    public void MoveAgent(ActionBuffers actionBuffers)
    {
        m_Shoot = false;

        if (Time.time > m_FrozenTime + 4f && m_Frozen)
        {
            Unfreeze();
        }
        if (Time.time > m_EffectTime + 0.5f)
        {
            if (m_Poisoned)
            {
                Unpoison();
            }
            if (m_Satiated)
            {
                Unsatiate();
            }
        }

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        if (!m_Frozen)
        {
            var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
            var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
            var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

            dirToGo = transform.forward * forward;
            dirToGo += transform.right * right;
            rotateDir = -transform.up * rotate;

            var shootCommand = discreteActions[0] > 0;
            if (shootCommand)
            {
                m_Shoot = true;
                dirToGo *= 0.5f;
                m_AgentRb.velocity *= 0.75f;
            }
            m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
            transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
        }

        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
        }

        if (m_Shoot)
        {
            var myTransform = transform;
            myLaser.transform.localScale = new Vector3(1f, 1f, m_LaserLength);
            var rayDir = 25.0f * myTransform.forward;
            Debug.DrawRay(myTransform.position, rayDir, Color.red, 0f, true);
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, 2f, rayDir, out hit, 25f))
            {
                if (hit.collider.gameObject.CompareTag("agent"))
                {
                    hit.collider.gameObject.GetComponent<FoodGathererAgent>().Freeze();
                }
            }
        }
        else
        {
            myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        }
    }

    void Freeze()
    {
        gameObject.tag = "frozenAgent";
        m_Frozen = true;
        m_FrozenTime = Time.time;
        gameObject.GetComponentInChildren<Renderer>().material = frozenMaterial;
    }

    void Unfreeze()
    {
        m_Frozen = false;
        gameObject.tag = "agent";
        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    }

    void Poison()
    {
        m_Poisoned = true;
        m_EffectTime = Time.time;
        gameObject.GetComponentInChildren<Renderer>().material = badMaterial;
    }

    void Unpoison()
    {
        m_Poisoned = false;
        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    }

    void Satiate()
    {
        m_Satiated = true;
        m_EffectTime = Time.time;
        gameObject.GetComponentInChildren<Renderer>().material = goodMaterial;
    }

    void Unsatiate()
    {
        m_Satiated = false;
        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);
    }

    // user controlled actions for agent
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
    }

    //reset agent parameters when new episode begins
    public override void OnEpisodeBegin()
    {   
        if(agentReset) { 
            Unfreeze();
            Unpoison();
            Unsatiate();
            m_Shoot = false;
            m_AgentRb.velocity = Vector3.zero;
            myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
            // transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
            //     2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            //     + area.transform.position;
            transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

            SetResetParameters();
            Debug.Log("Episode complete.");

            // Only reset the environment if Agent is training
        
            m_EnvironmentSettings.EnvironmentReset();
        }
    }

    //on collision with food, give reward
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("food") && this.gameObject.GetComponent<BehaviorManager>().canCarryMoreFood())
        {
            Debug.Log("food collision");
            if (collision.gameObject.GetComponent<FoodScript>() == null)
            {
                Debug.LogError("FoodScript is null on collided object!");
            }
            Satiate();
            collision.gameObject.GetComponent<FoodScript>().OnEaten();
            this.gameObject.GetComponent<BehaviorManager>().updateFoodCount();
            AddReward(1f);
            if (contribute)
            {
                Debug.Log("Contributing to foodScore");
                m_EnvironmentSettings.foodScore += 1;
            }
        }
        if (collision.gameObject.CompareTag("badFood"))
        {
            Poison();
            collision.gameObject.GetComponent<FoodScript>().OnEaten();

            AddReward(-1f);
            if (contribute)
            {
                Debug.Log("Contributing to foodScore");
                m_EnvironmentSettings.foodScore -= 1;
            }
        }

    }

    //laser for consuming food
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
