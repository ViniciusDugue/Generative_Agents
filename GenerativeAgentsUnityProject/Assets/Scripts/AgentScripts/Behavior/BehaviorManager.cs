using Unity.MLAgents;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class BehaviorManager : MonoBehaviour
{
    [Header("Basic Variables")]
    public int agentID = 001;
    private static int globalAgentID = 1;  // Shared counter for unique IDs
    public float fitnessScore = 0.0f;
    public float exhaustion;
    [SerializeField] public int RequiredFood => agentHeal.RequiredFood;
    [SerializeField] public int CurrentHunger => agentHeal.CurrentHunger;
    
    public bool refreshLLM = false;

    [Header("Advanced Variables")]
    [Tooltip("Maximum amount of food the agent can carry at once.")]
    [SerializeField]
    private int maxFood = 3;
    [Tooltip("Total amount of food the agent has collected Today.")]
    [SerializeField]
    private int foodCollected = 0;
    [Tooltip("Current amount of food items the agent has collected.")]
    [SerializeField]
    private int currentFood = 0;
    [SerializeField]
    public float FitnessScore;
    public AgentBehavior defaultBehavior;
    public AgentBehavior currentAgentBehavior;
    private Dictionary<string, AgentBehavior> behaviors = new Dictionary<string, AgentBehavior>();
    private Coroutine exhaustionCoroutine;
    private bool mapDataExist = false;
    public bool MapDataExist
{
    get => mapDataExist;
    set => mapDataExist = value;
}
    private bool _updateLLM = false;
    public delegate void updateLLMBoolChangedHandler(int agentID, bool mapData);
    public event updateLLMBoolChangedHandler OnUpdateLLM;
    private List<string> behaviorKeyList = new List<string>();
    private float raycastInterval = 0.2f; // Time between raycasts
    private float nextRaycastTime = 0.0f;
    [HideInInspector]
    public bool enemyCurrentlyDetected = false;     // Tracks whether an enemy is detected this frame
     [HideInInspector]
    public bool enemyPreviousDetected = false;      // Tracks whether an enemy was detected within a buffer time frame
     [HideInInspector]
    public Transform enemyTransform;
    [HideInInspector]
    public HashSet<Transform> activeFoodLocations = new HashSet<Transform>();
        [HideInInspector]
    public HashSet<Transform> foodLocations = new HashSet<Transform>();
    private float enemyOutOfRangeStartTime = -1f;
    private AgentHealth agentHealth;
    private AgentHeal agentHeal;
    private Habitat agentHabitat; 
    private GatherBehavior gatherBehavior; 
    public float depositedFood = 0;
    private bool hasDepositedFood = false;

    public AgentBehaviorUI agentBehaviorUI;
    public string reasoning;
    
    // NEW: Time tracking for enemy detection.
    private float enemyDetectionBuffer = 5f;

    // only count the very first discovery of each spawn, across all agents
    private static readonly HashSet<Transform> _globallyDiscoveredFoodSpawns = new();

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
                    _updateLLM = false;
                }
            }
        }
    }

    void Start()
    {
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
        StartCoroutine(pollLLM());

        // Get References
        agentHeal = GetComponent<AgentHeal>();
        agentHealth = GetComponent<AgentHealth>();
        gatherBehavior = GetComponent<GatherBehavior>();
        agentHabitat = GameObject.FindGameObjectWithTag("habitat").GetComponent<Habitat>();
    }

    private void OnEnable()
    {
        MarkerEventManager.OnMarkerRemoved += HandleMarkerRemoved;
    }

    private void OnDisable()
    {
        MarkerEventManager.OnMarkerRemoved -= HandleMarkerRemoved;
    }

    /// <summary>
    /// As soon as a FoodSpawn-point fires MarkerRemoved, drop it from both sets.
    /// </summary>
    private void HandleMarkerRemoved(GameObject obj)
    {
        Transform t = obj.transform;
        if (activeFoodLocations.Remove(t))
            Debug.Log($"[Agent {agentID}] Removed active food location {t.name}");

        if (foodLocations.Remove(t))
            Debug.Log($"[Agent {agentID}] Removed known food location {t.name}");
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
        calculateFitnessScore();
    }

    
    private void esclatedDetectedEnemyToLLM() {
        if (enemyCurrentlyDetected && !enemyPreviousDetected)
        {
            Debug.Log("Enemy just entered the radius. Prompting LLM instantly.");
            // Mark that an enemy is detected.
            enemyPreviousDetected = true;
            UpdateLLM = true;
            mapDataExist = false;
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
            enemyPreviousDetected = false;
            enemyOutOfRangeStartTime = -1f;
            UpdateLLM = true;
            mapDataExist = false;
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

        try
        {
            // Disable current AgentBehavior
            currentAgentBehavior.enabled = false;
        }
            catch (System.Exception e)
        {
            Debug.LogWarning($"Error disabling {currentAgentBehavior.GetType().Name}: {e.Message}");
        }
        
            // Switch and enable new AgentBehavior  
            currentAgentBehavior = newBehavior;
            currentAgentBehavior.enabled = true;

            agentBehaviorUI.UpdateBehaviorUI();

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

    private IEnumerator pollLLM()
{
    float interval = 10f;
    float timer = interval;
    bool lastUpdateLLM = UpdateLLM; // track the initial value

    while (refreshLLM)
    {
        // Check if the flag has changed since the last frame
        if (lastUpdateLLM != UpdateLLM)
        {
            timer = interval;  // reset the timer on any change
            lastUpdateLLM = UpdateLLM;
        }
        
        // Countdown the timer
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // If timer expires without interruption, set the flag to true.
            UpdateLLM = true;
            mapDataExist = true;
            timer = interval;  // reset the timer after triggering
            lastUpdateLLM = UpdateLLM; // update the last known value
        }
        
        yield return null;
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
        RayPerceptionSensorComponent3D[] rayPerceptionSensorComponents = GetComponents<RayPerceptionSensorComponent3D>();

        float maxDetectionDistance = 22.5f; // Set your max detection distance here
        enemyCurrentlyDetected = false;


        foreach (var m_rayPerceptionSensorComponent3D in rayPerceptionSensorComponents)
        {
            var rayOutputs = RayPerceptionSensor.Perceive(m_rayPerceptionSensorComponent3D.GetRayPerceptionInput(), true).RayOutputs;
            int lengthOfRayOutputs = rayOutputs.Length;

            // Alternating Ray Order: it gives an order of
            // (0, -delta, delta, -2delta, 2delta, ..., -ndelta, ndelta)
            // index 0 indicates the center of raycasts
            for (int i = 0; i < lengthOfRayOutputs; i++)
            {
                GameObject goHit = rayOutputs[i].HitGameObject;

                if (goHit != null) {

                    // Check if the hit object is a food spawn point
                    if (goHit.tag == "foodSpawn")
                    {
                        var status = goHit.GetComponent<FoodSpawnPointStatus>();
                        if (status != null && status.HasFood)
                        {
                            activeFoodLocations.Add(goHit.transform);

                            if (_globallyDiscoveredFoodSpawns.Add(goHit.transform))
                            {
                                EndSimMetricsUI.Instance.IncrementFoodLocationsDiscovered();
                                Debug.Log("Active Food location found!");
                            }
                        }
                    }

                    if(goHit.tag == "food")
                    {
                        if(gatherBehavior != null && gatherBehavior.enabled) {
                            gatherBehavior.isGathering = true;
                            gatherBehavior.SetFoodTarget(goHit);
                        }
                    }

                    // Check if the hit object is an enemy agent
                    if (goHit.tag == "enemyAgent" && rayOutputs[i].HitFraction <= maxDetectionDistance)
                    {
                        enemyTransform = goHit.transform;
                        enemyCurrentlyDetected = true;
                        // lastEnemyDetectionTime = Time.time;
                        //Debug.Log($"Enemies Detected by Agent {agentID}!");
                    }
                }
            }
        }
    }

    public float calculateFitnessScore() {
        float maxHealth = agentHealth.maxHealth;
        float curHealth = agentHealth.currentHealth;
        float habitatFood = agentHabitat.storedFood; 

        FitnessScore = 10 * habitatFood + 5 * currentFood + 7*(depositedFood)
        - 10 * (agentHealth.maxHealth -agentHealth.currentHealth);
        return FitnessScore;
    }


    public void updateFoodCount() {
        foodCollected += 1;
        currentFood += 1;
    }

    public bool canCarryMoreFood() {
        if (currentFood < maxFood)
        {
            return true;
        }
        return false;
    }

    public void dropFood() {
        depositedFood += 1;
        currentFood -= 1;
    }

    public int getFood() {
        return currentFood;
    }

    public int DepositAllFood()
    {
        if (currentFood > 0)
        {
            int deposited = currentFood;
            depositedFood += currentFood;
            currentFood = 0;
            return deposited;
        }
        else
        {
            // Already deposited, return 0 so it doesn't add more.
            return 0;
        }
    }

    public void eatPersonalFoodSupply() {
        if (CurrentHunger <= RequiredFood) {
            agentHeal.ReceiveFood(1);  // Increase hunger by 1 unit for each food consumed.= 1;
            currentFood -= 1;
        }
    }
    // Optionally, add a method to reset this flag for the next cycle/day.
    public void ResetFoodDepositFlag()
    {
        hasDepositedFood = false;
    } 

    public void ApplyDailyHungerPenalty()
    {
        // Using depositedFood as the count of food this agent deposited today.
        // int deposited = Mathf.RoundToInt(depositedFood);

        if (CurrentHunger < RequiredFood)
        {
            int missingFood = RequiredFood - CurrentHunger;
            // Parameter: Percentage of max health damage per missing food portion.
            float damagePercentagePerPortion = 0.10f;  // 10% of max health per missing food
            AgentHeal agentHeal = GetComponent<AgentHeal>(); // TODO: MODIFY AGENTHEAL TO CALL AGENTHEALTH INSTEAD
            AgentHealth agentHealth = GetComponent<AgentHealth>();
            if (agentHealth != null)
            {
                // Calculate total damage.
                int damage = Mathf.RoundToInt(missingFood * damagePercentagePerPortion * agentHealth.maxHealth);
                Debug.Log($"Agent {agentID} did not consume enough food. Missing {missingFood} portions. Applying {damage} damage.");
                agentHeal.TakeDamage(damage);
                agentHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("AgentHealth component missing on " + gameObject.name);
            }
        }
        else
        {
            Debug.Log($"Agent {agentID} met the food requirement with {RequiredFood} portions.");
        }

        // Reset the daily food tally for the next day.
        depositedFood = 0;
    }

    public void ClearDailyFoodLocations()
    {
        activeFoodLocations.Clear();
    }

}
