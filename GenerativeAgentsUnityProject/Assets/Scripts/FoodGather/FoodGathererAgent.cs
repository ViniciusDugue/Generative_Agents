using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using TMPro;

public class FoodGathererAgent : AgentBehavior
{
    public EnvironmentSettings m_EnvironmentSettings;
    public GameObject area;
    FoodSpawner m_MyArea;
    Rigidbody m_AgentRb;

    // Movement settings.
    public float turnSpeed = 300;
    public float moveSpeed = 2;
    public bool agentReset = true;

    public TextMeshProUGUI fitnessScoreText; 
    public int agentIndex; 
    private float fitnessScore = 0f;
    public bool useVectorObs;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<FoodSpawner>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
        UpdateFitnessScoreText();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;

        // move forward/backward and rotation handling
        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        rotateDir = -transform.up * rotate;

        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        // prevent speed from increasing too much
        if (m_AgentRb.velocity.sqrMagnitude > 25f)
        {
            m_AgentRb.velocity *= 0.95f;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //movement heuristics
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
    }

    public override void OnEpisodeBegin()
    {   
        if (agentReset)
        { 
            m_AgentRb.velocity = Vector3.zero;
            // respawn agent randomly
            transform.position = new Vector3(
                Random.Range(-m_MyArea.range, m_MyArea.range),
                2f,
                Random.Range(-m_MyArea.range, m_MyArea.range)
            ) + area.transform.position;
            transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

            SetResetParameters();
            Debug.Log("Episode complete.");

            // reset environment if training
            m_EnvironmentSettings.EnvironmentReset();
        }
    }

    void UpdateFitnessScoreText()
    {
        if (fitnessScoreText != null)
        {
            fitnessScoreText.text = "Agent " + agentIndex + " : " + fitnessScore.ToString("0.##");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("food"))
        {
            Debug.Log("Food collision");
            var foodScript = collision.gameObject.GetComponent<FoodScript>();
            if (foodScript == null)
            {
                Debug.LogError("FoodScript is null on collided object!");
            }
            else
            {
                foodScript.OnEaten();
                AddReward(1f);
                fitnessScore += 1f;
                UpdateFitnessScoreText();

                if (m_EnvironmentSettings != null)
                {
                    m_EnvironmentSettings.foodScore += 1;
                }
            }
        }
    }

    public void SetResetParameters()
    {
    }
}
