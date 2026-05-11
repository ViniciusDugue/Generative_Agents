using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBehaviorLegendUI : MonoBehaviour
{
    public GameObject uiPanel;
    public static AgentBehaviorLegendUI Instance;
    void Awake()
    {
        Instance = this;
        OpenUI(); // Start with it closed
    }
    
    public void OpenUI()
    {
        uiPanel.SetActive(true);
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
    }
}
