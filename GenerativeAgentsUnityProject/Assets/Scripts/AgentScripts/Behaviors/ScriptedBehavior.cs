using UnityEngine;

public abstract class ScriptedBehavior : IAgentBehavior
{
    public abstract void ExecuteBehavior(Agent agent);
}

