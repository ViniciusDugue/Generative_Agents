using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro; // Add this namespace for TextMeshPro

public class EnvironmentSettings : MonoBehaviour
{
    public GameObject[] agents;
    public FoodSpawner[] listArea;
    public int foodScore = 0;
    public TextMeshProUGUI foodScoreText; // Replace Text with TextMeshProUGUI

    StatsRecorder m_Recorder;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("food"));
        ClearObjects(GameObject.FindGameObjectsWithTag("badFood"));

        agents = GameObject.FindGameObjectsWithTag("agent");
        listArea = FindObjectsOfType<FoodSpawner>();
        foreach (var fa in listArea)
        {
            fa.ResetFoodArea(agents);
        }

        foodScore = 0;
    }

    void ClearObjects(GameObject[] objects)
    {
        foreach (var food in objects)
        {
            Destroy(food);
        }
    }

    public void Update()
    {
        foodScoreText.text = $"Food Collected: {foodScore}";

        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("Food Score", foodScore);
        }
    }
}
