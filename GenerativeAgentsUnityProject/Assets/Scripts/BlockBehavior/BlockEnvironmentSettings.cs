using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class BlockEnvironmentSettings : MonoBehaviour
{
    public BlockSpawner[] listArea;// every environment is defined by this 
    public int blockScore = 0;

    StatsRecorder m_Recorder;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("destination")); //Clear existing destinations
        ClearObjects(GameObject.FindGameObjectsWithTag("block")); // Clear existing blocks
        
        listArea = FindObjectsOfType<BlockSpawner>();
        
        Debug.Log("Environment reset complete.");
        foreach (var spawner in listArea)
        {   
            spawner.spawnedBlocks.Clear();
            spawner.ResetBlockArea();
            
        }

        blockScore = 0;
        
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
        if (Input.GetKey(KeyCode.Alpha9) && ((Time.frameCount % 60) == 0))
        {
            Debug.Log("manually reset environments");
            EnvironmentReset();
        }
        
        // if ((Time.frameCount % 100) == 0)
        // {
        //     m_Recorder.Add("Food Score", blockScore);
        // }
    }
}
