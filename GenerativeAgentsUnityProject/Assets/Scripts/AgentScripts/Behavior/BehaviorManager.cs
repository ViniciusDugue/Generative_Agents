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
        Debug.Log("Update is running");  // <- Add this line to check if Update is firing

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed");  // <- Check if Unity registers the key press

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
