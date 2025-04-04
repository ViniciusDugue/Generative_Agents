using Unity.MLAgents;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class BehaviorManager : MonoBehaviour
{
    public int agentID = 001;
    private static int globalAgentID = 1;  // Shared counter for unique IDs

    public float exhaustion;
    private GameObject agentObject;
    public AgentBehavior defaultBehavior;
    public AgentBehavior currentAgentBehavior;
    private Dictionary<string, AgentBehavior> behaviors = new Dictionary<string, AgentBehavior>();
    private Coroutine exhaustionCoroutine;
    private bool mapDataExist = false;
    private bool _updateLLM = false;
    public delegate void updateLLMBoolChangedHandler(int agentID, bool mapData);
    public event updateLLMBoolChangedHandler OnUpdateLLM;
    private List<string> behaviorKeyList = new List<string>();
    private float raycastInterval = 0.2f; // Time between raycasts
    private float nextRaycastTime = 0.0f;
    
    [HideInInspector]
    public HashSet<Transform> foodLocations = new HashSet<Transform>();


    public bool UpdateLLM
    {
        get { return _updateLLM; }
        set
        {
            if (_updateLLM != value)
            {
                _updateLLM = value;
                if (_updateLLM) // Only trigger when set to true
                {
                    // Debug.Log($"Invoking OnUpdateLLM for Agent {agentID}");
                    OnUpdateLLM?.Invoke(agentID, mapDataExist);
                }
            }
        }
    }

    void Start()
    {
        agentObject = this.gameObject;

        // Populate the dictionary with all Agent components, using their script names as keys
        foreach (AgentBehavior agentBehavior in GetComponents<AgentBehavior>())
        {
            string agentName = agentBehavior.GetType().Name; // Get the script name
            behaviors[agentName] = agentBehavior;
            behaviorKeyList.Add(agentName);
            Debug.Log($"Registered AgentBehavior: {agentName}");
        }

        // Ensure the default AgentBehavior is valid
        if (defaultBehavior == null || !behaviors.ContainsKey(defaultBehavior.GetType().Name))
        {
            Debug.LogWarning("Default AgentBehavior not found in attached agents. Using first found Agent as default.");
            defaultBehavior = GetFirstBehavior();
        }

        currentAgentBehavior = defaultBehavior;
        Debug.Log($"Behavior Manager initialized with {behaviors.Count} behaviors.");

        // Start Exhaustion counter
        StartExhaustionCoroutine();
    }

    private void Update()
    {
        // Ensure current AgentBehavior is not null
        if (currentAgentBehavior == null)
        {
            Debug.LogError("Current Agent AgentBehavior is null");
            return;
        }

        // Check raycasts periodically to increase Performance
        if (Time.time >= nextRaycastTime)
        {   
            checkRayCast();
            nextRaycastTime = Time.time + raycastInterval;
        }

        // Switch between behaviors using behavior names
        if (Input.GetKeyDown(KeyCode.Q)) // Example: Switch to first behavior
        {
            SwitchBehavior(GetFirstBehavior().GetType().Name);
        }
        if (Input.GetKeyDown(KeyCode.E)) // Example: Switch to second behavior
        {
            SwitchBehavior(GetNextBehaviorName());
        }
        if (Input.GetKeyDown(KeyCode.R)) // Example: Switch to second behavior
        {
            MapEncoder mapEncoder = GetComponent<MapEncoder>();

            if (mapEncoder != null && mapEncoder.isActiveAndEnabled)
            {
                // Set Boolean Listeners to True
                mapDataExist = true;
                UpdateLLM = true;
                Debug.Log($"Map captured and sent by Agent {agentID}");
            }
            else
            {
                mapDataExist = true;
                UpdateLLM = true;
                Debug.Log($"No Map was captured by Agent {agentID}");
            }

            Debug.Log($"UpdateLLM set to: {UpdateLLM}");
        }
    }

    public void InitializeAgent()
    {
        agentID = globalAgentID++;
        Debug.Log($"Agent {agentID} initialized.");
    }

    public void SwitchBehavior(string behaviorName)
    {
        if (behaviors.TryGetValue(behaviorName, out AgentBehavior newBehavior))
        {
            Debug.Log($"Switching AgentBehavior to {behaviorName}");

            // Disable current AgentBehavior
            currentAgentBehavior.enabled = false;

            // Switch and enable new AgentBehavior  
            currentAgentBehavior = newBehavior;
            currentAgentBehavior.enabled = true;

            // Stop previous exhaustion coroutine and restart with the new exhaustion rate
            StartExhaustionCoroutine();
        }
        else
        {
            Debug.LogWarning($"Behavior '{behaviorName}' not found.");
        }
    }

    public void SetMoveTarget(object coords)
    {
        // Validate the input to ensure it is not null
        if (coords == null)
        {
            return;
        }

        // Ensure the coords object is a JObject (JSON object)
        if (coords is Newtonsoft.Json.Linq.JObject locationDict)
        {
            try
            {
                // Parse location values safely
                float x = locationDict["x"].ToObject<float>();
                float y = this.transform.position.y; // Use the current y position
                float z = locationDict["z"].ToObject<float>();

                // Convert to Vector3
                Vector3 targetLocation = new Vector3(x, y, z);
                Debug.Log($"Target Location: {targetLocation}");

                // Assign target position correctly
                if (behaviors.ContainsKey("MoveBehavior"))
                {
                    MoveBehavior moveBehavior = (MoveBehavior)behaviors["MoveBehavior"];
                    moveBehavior.setTarget(targetLocation);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing location: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("SetMoveTarget received an invalid location format.");
        }

    }
        

    private void StartExhaustionCoroutine()
    {
        // Stop any existing exhaustion coroutine before starting a new one
        if (exhaustionCoroutine != null)
        {
            StopCoroutine(exhaustionCoroutine);
        }

        // Start new coroutine with updated exhaustion rate
        exhaustionCoroutine = StartCoroutine(UpdateExhaustion());
    }

    private IEnumerator UpdateExhaustion()
    {
        while (true)
        {

            yield return new WaitForSeconds(1.0f);
            if ((exhaustion + currentAgentBehavior.exhaustionRate) > 0)
                exhaustion += currentAgentBehavior.exhaustionRate; 
            else // Ensure exhaustion does not go below 0
               exhaustion = 0; 
        }
    }


    private string GetNextBehaviorName()
    {
        
        // Get the index of the current behavior
        int currentIndex = behaviorKeyList.IndexOf(currentAgentBehavior.GetType().Name);
        
        // Calculate the index of the next behavior
        int nextIndex = (currentIndex + 1) % behaviorKeyList.Count;
        
        // Return the key of the next behavior
        return behaviorKeyList[nextIndex];
    }

    private AgentBehavior GetFirstBehavior()
    {
        foreach (var behavior in behaviors.Values)
        {
            return behavior;
        }
        return null;
    }

    // check raycast hit info
    private void checkRayCast()
    {
        RayPerceptionSensorComponent3D m_rayPerceptionSensorComponent3D = GetComponent<RayPerceptionSensorComponent3D>();

        var rayOutputs = RayPerceptionSensor.Perceive(m_rayPerceptionSensorComponent3D.GetRayPerceptionInput(), true).RayOutputs;
        int lengthOfRayOutputs = rayOutputs.Length;

        // Alternating Ray Order: it gives an order of
        // (0, -delta, delta, -2delta, 2delta, ..., -ndelta, ndelta)
        // index 0 indicates the center of raycasts
        for (int i = 0; i < lengthOfRayOutputs; i++)
        {
            GameObject goHit = rayOutputs[i].HitGameObject;
            if (goHit != null && goHit.tag == "foodSpawn")
            {
                if (foodLocations.Add(goHit.transform)) // Add returns false if the item is already present
                {
                    Debug.Log("Food location found!");
                }
            }
        }
    }
}
