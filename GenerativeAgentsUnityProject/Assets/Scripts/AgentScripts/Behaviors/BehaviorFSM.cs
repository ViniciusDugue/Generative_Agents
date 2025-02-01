using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using System.Collections.Generic;

public enum AgentState
{
    RL_CollectFood,
    S_Rest
}

public class BehaviorFSM : MonoBehaviour
{
    private Agent activeAgent;
    private BehaviorParameters behaviorParameters;

    private Dictionary<AgentState, IAgentBehavior> behaviors;
    private Dictionary<AgentState, Agent> agentScripts;

    private IAgentBehavior currentBehavior;
    private AgentState currentState;

   void Start()
    {
        // Reference to all possible agents (disable them initially)
        agentScripts = new Dictionary<AgentState, Agent>
        {
            { AgentState.RL_CollectFood, GetComponent<FoodGathererAgent>() },
            { AgentState.RL_AvoidObstacles, GetComponent<AvoidObstaclesAgent>() }
        };

        foreach (var agent in agentScripts.Values)
        {
            if (agent != null)
                agent.enabled = false; // Disable all agents at the start
        }

        behaviorParameters = GetComponent<BehaviorParameters>();

        // Initialize behaviors with correct model paths
        behaviors = new Dictionary<AgentState, IAgentBehavior>
        {
            { AgentState.RL_CollectFood, new RLBehavior("Models/FoodGathering") },
            { AgentState.RL_AvoidObstacles, new RLBehavior("Models/AvoidObstacles") },
            { AgentState.Scripted_Custom, new ScriptedBehavior() }
        };

        // Default behavior
        ChangeState(AgentState.RL_CollectFood);
    }

    void Update()
    {
        ActionBuffers actionBuffers = new ActionBuffers(new float[3], new int[1]); // Empty buffer
        currentBehavior.ExecuteBehavior(activeAgent, actionBuffers);

        // Manual switching for testing
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeState(AgentState.RL_CollectFood);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeState(AgentState.S_Rest);
    }

    public void ChangeState(AgentState newState)
    {
        if (currentState == newState) return;

        // Disable the previous agent (if any)
        if (activeAgent != null)
        {
            activeAgent.enabled = false;
        }

        // Set new state
        currentState = newState;
        currentBehavior = behaviors[newState];

        // Activate the correct agent
        if (agentScripts.ContainsKey(newState))
        {
            activeAgent = agentScripts[newState];
            activeAgent.enabled = true;
            behaviorParameters = activeAgent.GetComponent<BehaviorParameters>();

            // Ensure correct ONNX model is assigned
            if (behaviorParameters != null && newState != AgentState.Scripted_Custom)
            {
                ((RLBehavior)currentBehavior).ExecuteBehavior(activeAgent, new ActionBuffers(new float[3], new int[1]));
            }
        }

        Debug.Log($"Switched to {newState}");
    }
}