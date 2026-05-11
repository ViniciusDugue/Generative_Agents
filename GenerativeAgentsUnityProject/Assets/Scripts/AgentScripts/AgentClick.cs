using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentClick : MonoBehaviour
{
    private AgentHealth healthComp;

    void Awake()
    {
        healthComp = GetComponent<AgentHealth>();
    }

    void OnMouseDown()
    {
        var health = GetComponent<AgentHealth>();
        var behavior = GetComponent<BehaviorManager>();
        if (health != null && behavior != null)
        {
            AgentUIManager.Instance.ShowAgentStats(health, behavior);
        }
    }
}
