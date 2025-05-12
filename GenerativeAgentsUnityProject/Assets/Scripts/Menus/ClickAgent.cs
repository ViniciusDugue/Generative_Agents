using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class ClickAgent : MonoBehaviour
{
    public BehaviorManager behaviorManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject clickedObject = hit.collider.gameObject;

                if (clickedObject.CompareTag("agent")) // Check if the tag is "Agent"
                {
                    behaviorManager = clickedObject.GetComponent<BehaviorManager>();
                    Debug.Log("Agent Clicked");
                    if (behaviorManager != null)
                    {
                        string reasoning = behaviorManager.reasoning;
                        AgentReasoningUI.Instance.SetText(reasoning);
                        AgentReasoningUI.Instance.OpenUI();
                    }
                    else
                    {
                        Debug.LogWarning("Clicked object has no BehaviorManager component!");
                    }
                }
            }
        }
        if(behaviorManager != null) {
            string reasoning = behaviorManager.reasoning;
            AgentReasoningUI.Instance.SetText(reasoning);
        }
    }
}
