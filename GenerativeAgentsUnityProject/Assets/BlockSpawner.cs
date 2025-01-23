using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{

    [SerializeField] public float range;
    [SerializeField] public GameObject blockPrefab;
    [SerializeField] public int blockCount;

    [SerializeField] public int blocksPerAgent;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("7 Blocks Created");
            CreateBlocks(7, blockPrefab);
        }
    }

    public void CreateBlocks(int num, GameObject type)
    {
        blockCount += num;
        for (int i = 0; i < num; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(-range, range),0f, Random.Range(-range, range)) + transform.position;

            GameObject f = Instantiate(type, 
                randomPosition,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));

            //Debug.Log($"Block spawned at location: {randomPosition}");
            // f.GetComponent<FoodScript>().respawn = respawnFood;
            // f.GetComponent<FoodScript>().myArea = this;
        }
    }

    public void ResetBlockArea(GameObject[] agents)
    {
        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 2f,
                    Random.Range(-range, range))
                    + transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }

        CreateBlocks(blocksPerAgent, blockPrefab);
    }
}
