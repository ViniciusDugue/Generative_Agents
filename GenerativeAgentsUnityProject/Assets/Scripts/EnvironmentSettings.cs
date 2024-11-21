using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class EnvironmentSettings : MonoBehaviour
{
    public GameObject[] enemyAgents;

    public GameObject[] agents;
    public FoodSpawner[] listArea;
    public int foodScore = 0;
    public Text foodScoreText;

    StatsRecorder m_Recorder;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("food"));
        ClearObjects(GameObject.FindGameObjectsWithTag("badFood"));

        agents = GameObject.FindGameObjectsWithTag("agent");
        enemyAgents = GameObject.FindGameObjectsWithTag("enemyAgent");
        listArea = FindObjectsOfType<FoodSpawner>();
        foreach (var fa in listArea)
        {
            fa.ResetFoodArea(agents);
            fa.ResetFoodArea(enemyAgents);
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
