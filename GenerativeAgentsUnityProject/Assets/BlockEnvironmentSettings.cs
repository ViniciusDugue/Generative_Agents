using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class BlockEnvironmentSettings : MonoBehaviour
{
    public GameObject[] enemyAgents;

    public GameObject[] agents;
    public BlockSpawner[] listArea;
    public int blockScore = 0;

    StatsRecorder m_Recorder;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("block")); // Clear existing blocks

        agents = GameObject.FindGameObjectsWithTag("agent");
        enemyAgents = GameObject.FindGameObjectsWithTag("enemyAgent");
        listArea = FindObjectsOfType<BlockSpawner>();

        foreach (var spawner in listArea)
        {
            spawner.ResetBlockArea(agents);
        }

        foreach (var agent in agents)
        {
            BlockAgent blockAgent = agent.GetComponent<BlockAgent>();
            if (blockAgent != null)
            {
                Vector3 randomPosition = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                blockAgent.targetBlockCurrentPos = new Vector2(randomPosition.x, randomPosition.z);

                Vector3 destinationPosition = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                blockAgent.targetBlockDestinationPos = new Vector2(destinationPosition.x, destinationPosition.z);
            }
        }

        blockScore = 0;
        Debug.Log("Environment reset complete.");
    }

    void ClearObjects(GameObject[] objects)
    {
        foreach (var block in objects)
        {
            Destroy(block);
        }
    }

    public void Update()
    {
        // if ((Time.frameCount % 100) == 0)
        // {
        //     m_Recorder.Add("Food Score", blockScore);
        // }
    }
}
