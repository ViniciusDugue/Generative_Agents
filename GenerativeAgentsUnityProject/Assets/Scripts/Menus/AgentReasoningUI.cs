using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for Text
using TMPro;

public class AgentReasoningUI : MonoBehaviour
{
    public static AgentReasoningUI Instance;

    public GameObject uiPanel; // Drag your Reasoning UI Panel here in the Inspector
    public TMP_Text reasoningText; // Drag your Text (for showing reasoning) here in the Inspector

    public GameObject openButton;

    void Awake()
    {
        // start closed, but show the open button
        uiPanel.SetActive(false);
        openButton.SetActive(false);
    }

    public void SetText(string reasoning)
    {
        reasoningText.text = reasoning;
    }

    public void OpenUI()
    {
        uiPanel.SetActive(true);
        openButton.SetActive(false);
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
        openButton.SetActive(true);
    }
}
