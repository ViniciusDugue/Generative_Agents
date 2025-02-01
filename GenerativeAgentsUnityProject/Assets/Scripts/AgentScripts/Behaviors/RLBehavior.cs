using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public class RLBehavior : IAgentBehavior
{
    private string modelName;
    private ModelAsset modelAsset;

    public RLBehavior(string modelPath)
    {
        modelName = modelPath;
        modelAsset = Resources.Load<ModelAsset>(modelPath); // Load ONNX model from Resources folder
    }

    public void ExecuteBehavior(Agent agent, ActionBuffers actionBuffers)
    {
        var behaviorParameters = agent.GetComponent<BehaviorParameters>();
        
        if (behaviorParameters != null && modelAsset != null)
        {
            behaviorParameters.Model = modelAsset;  // Assign the correct trained model
            behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
        }

        agent.OnActionReceived(actionBuffers); // Execute RL behavior
    }
}
