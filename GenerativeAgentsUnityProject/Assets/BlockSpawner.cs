using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{

    [SerializeField] public float range;
    [SerializeField] public GameObject blockPrefab;
    [SerializeField] public int blockCount;

    [SerializeField] public int blocksPerAgent;

    private List<GameObject> spawnedBlocks = new List<GameObject>();
    
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
        foreach (GameObject block in spawnedBlocks)
        {
            Destroy(block);
        }
        spawnedBlocks.Clear();

        foreach (GameObject agent in agents)
        {
            for (int i = 0; i < blocksPerAgent; i++)
            {
                Vector3 randomPosition = new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range)) + transform.position;
                GameObject block = Instantiate(blockPrefab, randomPosition, Quaternion.identity);
                spawnedBlocks.Add(block);
            }
        }
    }
}
