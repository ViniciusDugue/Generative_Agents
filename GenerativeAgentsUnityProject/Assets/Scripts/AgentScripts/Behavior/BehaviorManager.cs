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
    private Coroutine enemyBufferCoroutine;

    private bool _updateLLM = false;
    public delegate void updateLLMBoolChangedHandler(int agentID);
    public event updateLLMBoolChangedHandler OnUpdateLLM;
    private List<string> behaviorKeyList = new List<string>();
    private float raycastInterval = 0.2f; // Time between raycasts
    private float nextRaycastTime = 0.0f;
    private float lastEnemyLogTime = -100f;
    private float enemyLogInterval = 2f;
    private bool enemyCurrentlyDetected = false;     // Tracks whether an enemy is detected this frame
    private bool enemyPreviousDetected = false;      // Tracks whether an enemy was detected within a buffer time frame
    private float enemyOutOfRangeStartTime = -1f;
    

    
    
    [HideInInspector]
    public HashSet<Transform> foodLocations = new HashSet<Transform>();
    // NEW: Time tracking for enemy detection.
    private float lastEnemyDetectionTime;
    // You can adjust this radius as needed.
    private float enemyDetectionRadius = 10f;
    // The buffer time after which we consider that no enemy has been detected.
    private float enemyDetectionBuffer = 5f;


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
                    OnUpdateLLM?.Invoke(agentID);
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
            Debug.LogError("Current AgentBehavior is null");
            return;
        }

        // Check raycasts periodically to increase performance
        if (Time.time >= nextRaycastTime)
        {   
            checkRayCast();
            nextRaycastTime = Time.time + raycastInterval;
            esclatedDetectedEnemyToLLM();
            // enemyPreviousDetected = enemyCurrentlyDetected; 
        }

        // Update enemy detection.
        // CheckEnemyDetection();

        // Determine whether an enemy is currently detected.
        
        

        // (The rest of your Update code for manual behavior switching remains unchanged)
        if (Input.GetKeyDown(KeyCode.Q)) // Example: Switch to first behavior
        {
            SwitchBehavior(GetFirstBehavior().GetType().Name);
        }
        if (Input.GetKeyDown(KeyCode.E)) // Example: Switch to second behavior
        {
            SwitchBehavior(GetNextBehaviorName());
        }
        if (Input.GetKeyDown(KeyCode.R)) // Example: Manual trigger of LLM update
        {
            UpdateLLM = true;
            MapEncoder mapEncoder = GetComponent<MapEncoder>();

            if (UpdateLLM && mapEncoder != null)
            {
                mapEncoder.CaptureAndSendMap(agentID);
                Debug.Log($"Map captured and sent by Agent {agentID}");
            }

            Debug.Log($"UpdateLLM set to: {UpdateLLM}");
        }
    }

    
    private void esclatedDetectedEnemyToLLM() {
        if (enemyCurrentlyDetected && !enemyPreviousDetected)
        {
            Debug.Log("Enemy just entered the radius. Prompting LLM instantly.");
            UpdateLLM = true;
            // Mark that an enemy is detected.
            enemyPreviousDetected = true;
        }
        else if (enemyCurrentlyDetected && enemyPreviousDetected) 
        {
            enemyOutOfRangeStartTime = -1f;
        }
        // Enemy has just left detection
        else if (!enemyCurrentlyDetected && enemyPreviousDetected && enemyOutOfRangeStartTime == -1f)
        {
            Debug.Log("Enemy no longer visible. Starting buffer timer.");
            enemyOutOfRangeStartTime = Time.time;
        }
        // If an enemy was detected in the previous frame but now is gone,
        // wait until the buffer period (2.5 sec) has passed before prompting. 
        if (!enemyCurrentlyDetected && enemyPreviousDetected && (Time.time - enemyOutOfRangeStartTime) >= enemyDetectionBuffer) 
        {       
            Debug.Log("Enemy recently left (buffer passed). Prompting LLM.");
            UpdateLLM = true;
            enemyPreviousDetected = false;
            enemyOutOfRangeStartTime = -1f;
            // lastLLMPromptTime = Time.time;
            
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
                float y = locationDict["y"].ToObject<float>();
                float z = locationDict["z"].ToObject<float>();

                // Convert to Vector3
                Vector3 targetLocation = new Vector3(x, y, z);
                Debug.Log($"Target Location: {targetLocation}");

                // Assign target position correctly
                if (behaviors.ContainsKey("MoveBehavior") && behaviors["MoveBehavior"] is MoveBehavior moveBehavior)
                {
                    moveBehavior.target = targetLocation;
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
            if (currentAgentBehavior != null)  // ✅ Added Null Check
            {
                exhaustion += 1;  // Example logic
                // Debug.Log($"Agent {agentID} exhaustion: {exhaustion}");
            }
            else
            {
                Debug.LogWarning($"Agent {agentID} has no current behavior assigned.");
            }

            yield return new WaitForSeconds(5f);
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
        float maxDetectionDistance = 20.5f; // Set your max detection distance here
        enemyCurrentlyDetected = false;

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
            if (goHit != null && goHit.tag == "enemyAgent" && rayOutputs[i].HitFraction <= maxDetectionDistance)
            {
                enemyCurrentlyDetected = true;
                // lastEnemyDetectionTime = Time.time;
                Debug.Log($"Enemies Detected by Agent {agentID}!");
            }

        }
    }
}
