using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AgentBehaviorUI : MonoBehaviour
{
    public Image behaviorIcon;
    public TMP_Text behaviorText;
    public BehaviorDatabase behaviorDatabase; // ScriptableObject with a list

    public BehaviorManager behaviorManager;

    public static AgentBehaviorUI Instance;

    public void Awake()
    {
        Instance = this;
    }
    public void UpdateBehaviorUI()
    {
        
        string behaviorName = behaviorManager.currentAgentBehavior.GetType().Name;
        Debug.Log($"agent Ui updating. new Behavior is: {behaviorName}");
        BehaviorVisual foundVisual = null;

        foreach (BehaviorVisual visual in behaviorDatabase.visuals)
        {
            if (visual.behaviorName == behaviorName)
            {
                foundVisual = visual;
                break;
            }
        }

        if (foundVisual != null)
        {
            behaviorIcon.sprite = foundVisual.icon;
            behaviorText.text = foundVisual.displayName;
        }
        else
        {
            
            // Fallback to default visual
            behaviorIcon.sprite = behaviorDatabase.defaultVisual.icon;
            behaviorText.text = behaviorDatabase.defaultVisual.displayName;
        }
    }

    public void UpdateAgentBehaviorUI(string behaviorName)
    {
        
        // string behaviorName = behaviorManager.currentAgentBehavior.GetType().Name;
        Debug.Log($"agent Ui updating. new Behavior is: {behaviorName}");
        BehaviorVisual foundVisual = null;

        foreach (BehaviorVisual visual in behaviorDatabase.visuals)
        {
            if (visual.behaviorName == behaviorName)
            {
                foundVisual = visual;
                break;
            }
        }

        if (foundVisual != null)
        {
            behaviorIcon.sprite = foundVisual.icon;
            behaviorText.text = foundVisual.displayName;
        }
        else
        {
            
            // Fallback to default visual
            behaviorIcon.sprite = behaviorDatabase.defaultVisual.icon;
            behaviorText.text = behaviorDatabase.defaultVisual.displayName;
        }
    }
}
