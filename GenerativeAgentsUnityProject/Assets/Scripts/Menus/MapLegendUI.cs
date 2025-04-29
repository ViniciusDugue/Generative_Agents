using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLegendUI : MonoBehaviour
{
    public GameObject uiPanel;
    public static MapLegendUI Instance;
    void Awake()
    {
        Instance = this;
        OpenUI(); 
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
