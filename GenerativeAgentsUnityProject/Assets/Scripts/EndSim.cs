using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndSim : MonoBehaviour
{
    public Button finishButton;
    public GameObject endMetrics;
    public GameObject runtimeUI;

    void Start() {
         GameObject endMetrics = this.gameObject.transform.GetChild(0).gameObject;
         GameObject runtimeUI = this.gameObject.transform.GetChild(1).gameObject;

        if (finishButton != null)
            finishButton.onClick.AddListener(TaskOnClick);
        else
            Debug.LogError("Finish Button is not assigned!");

        if (endMetrics == null)
            Debug.LogError("End Metrics GameObject is not assigned!");

        if (runtimeUI == null)
            Debug.LogError("Runtime UI GameObject is not assigned!");

        Debug.Log("End Metrics Active: " + endMetrics.activeSelf);
        Debug.Log("Runtime UI Active: " + runtimeUI.activeSelf);
    }

    void TaskOnClick() {
        Debug.Log("TaskOnClick Executed");
        Debug.Log(runtimeUI != null);
        Debug.Log(runtimeUI.activeSelf);
        if (runtimeUI != null) {
            runtimeUI.SetActive(false);
            Debug.Log("Set Runtime to False");
            if (endMetrics != null)
                endMetrics.SetActive(true);
                Debug.Log("Set endMetrics to True");
        }
    }
}
