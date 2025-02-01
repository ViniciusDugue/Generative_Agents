using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public interface IAgentBehavior
/*
Defines a execution function for all behaviors
*/
{
    void ExecuteBehavior(Agent agent, ActionBuffers actionBuffers);
}
