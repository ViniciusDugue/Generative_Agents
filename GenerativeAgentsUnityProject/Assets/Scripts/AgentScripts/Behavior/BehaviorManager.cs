using Unity.MLAgents;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

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
    private bool _updateLLM = false;
    public delegate void updateLLMBoolChangedHandler(int agentID);
    public event updateLLMBoolChangedHandler OnUpdateLLM;

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
                    OnUpdateLLM?.Invoke(agentID);
                }
            }
        }
    }

    void Start()
    {
        agentObject = this.gameObject;

        // Populate the dictionary with all AgentBehavior components on this GameObject
        foreach (AgentBehavior agentBehavior in GetComponents<AgentBehavior>())
        {
            string agentName = agentBehavior.GetType().Name;
            behaviors[agentName] = agentBehavior;
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

        // Start exhaustion counter
        StartExhaustionCoroutine();
    }

    private void Update()
    {
        Debug.Log("Update is running");

        // Check predator proximity and switch behaviors using hysteresis:
        // If a predator is within the trigger radius and we're not already fleeing, switch to flee.
        if (currentAgentBehavior.GetType().Name != "FleeBehaviorAgent" && ShouldFleeTrigger())
        {
            Debug.Log("Predator detected within trigger radius! Switching to FleeBehaviorAgent.");
            SwitchBehavior("FleeBehaviorAgent");
        }
        // If we're in flee mode and no predator is within the cancel radius, switch back.
        else if (currentAgentBehavior.GetType().Name == "FleeBehaviorAgent" && ShouldCancelFlee())
        {
            Debug.Log("No predator within cancel radius. Switching back to FoodGathererAgent.");
            SwitchBehavior("FoodGathererAgent");
        }

        // (Optional) Example of triggering an update from input:
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed");
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

            // Disable current behavior and enable the new one
            currentAgentBehavior.enabled = false;
            currentAgentBehavior = newBehavior;
            currentAgentBehavior.enabled = true;

            // Restart the exhaustion coroutine
            StartExhaustionCoroutine();
        }
        else
        {
            Debug.LogWarning($"Behavior '{behaviorName}' not found.");
        }
    }

    // Returns true if any predator is within the "trigger" radius (e.g., 7 units)
    private bool ShouldFleeTrigger()
    {
        float triggerRadius = 7f;
        string enemyTag = "enemyAgent";
        Collider[] colliders = Physics.OverlapSphere(transform.position, triggerRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag(enemyTag))
            {
                return true;
            }
        }
        return false;
    }

    // Returns true if no predator is detected within the "cancel" radius (e.g., 12 units)
    private bool ShouldCancelFlee()
    {
        float cancelRadius = 12f;
        string enemyTag = "enemyAgent";
        Collider[] colliders = Physics.OverlapSphere(transform.position, cancelRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag(enemyTag))
            {
                return false;
            }
        }
        return true;
    }

    private void StartExhaustionCoroutine()
    {
        if (exhaustionCoroutine != null)
        {
            StopCoroutine(exhaustionCoroutine);
        }
        exhaustionCoroutine = StartCoroutine(UpdateExhaustion());
    }

    private IEnumerator UpdateExhaustion()
    {
        while (true)
        {
            if (currentAgentBehavior != null)
            {
                exhaustion += 1;  // Example logic for increasing exhaustion
                Debug.Log($"Agent {agentID} exhaustion: {exhaustion}");
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
        foreach (var key in behaviors.Keys)
        {
            if (key != currentAgentBehavior.GetType().Name)
            {
                return key;
            }
        }
        return currentAgentBehavior.GetType().Name;
    }

    private AgentBehavior GetFirstBehavior()
    {
        foreach (var behavior in behaviors.Values)
        {
            return behavior;
        }
        return null;
    }
}
