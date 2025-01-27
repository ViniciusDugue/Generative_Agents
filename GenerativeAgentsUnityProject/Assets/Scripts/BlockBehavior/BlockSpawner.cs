using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BlockSpawner : MonoBehaviour
{

    [SerializeField] public float range;
    [SerializeField] public GameObject blockPrefab;
    [SerializeField] public int blockCount;
    [SerializeField] public int blocksPerAgent;
    [SerializeField] public GameObject destinationPrefab;

    public GameObject[] agents;
    public List<GameObject> spawnedBlocks = new List<GameObject>();
    [SerializeField] public bool isTraining;
    [SerializeField] private Material targetBlockMaterial;
    [SerializeField] public Material blockMaterial;
    [SerializeField] private bool blocksCreated = false;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            //CreateBlocks(5);
        }
    }

    //create n blocks, a target block, and destination for each agent and give agent necessary variables
    public void CreateObjects(int blocksPerAgent)
    {
        
        //find all agent gameobjects in single environment
        agents = GetComponentsInChildren<Transform>()
        .Where(t => t != transform && t.CompareTag("agent"))
        .Select(t => t.gameObject)
        .ToArray(); 

        blockCount = 0;
        
        foreach (var agent in agents)
        {
            BlockAgent blockAgent = agent.GetComponent<BlockAgent>();

            //clear this list for this agents environment reset
            blockAgent.spawnedBlocksPerAgent.Clear();
            
            //spawn in blocks per agent
            for (int i = 0; i < blocksPerAgent; i++)
            {
                blockCount += 1;
                Vector3 randomPosition = new Vector3(Random.Range(-range, range),1f, Random.Range(-range, range)) + transform.position;
                GameObject block = Instantiate(
                blockPrefab, 
                randomPosition,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)),
                transform
                );
                
                //update block lists for environment and per agent
                spawnedBlocks.Add(block);
                blockAgent.spawnedBlocksPerAgent.Add(block);
                
                
            }

            // if training, set the targetblock and give unique color
            // instantiate one destination hitbox per agent and assign the value
            if (isTraining)
            {
                //choose a random target block from each agents spawnedBlocksPerAgent list 
                blockAgent.targetBlock = blockAgent.spawnedBlocksPerAgent[Random.Range(0, blockAgent.spawnedBlocksPerAgent.Count)];

                //give target block unique color
                SetBlockMaterial(blockAgent.targetBlock, targetBlockMaterial);

                // Instantiate destination hitbox and move randomly
                Vector3 destinationPosition = new Vector3(Random.Range(-range, range), 3f, Random.Range(-range, range)) + transform.position;
                blockAgent.targetBlockDestinationPos = new Vector2(destinationPosition.x, destinationPosition.z);
                blockAgent.destinationObject = Instantiate(destinationPrefab, destinationPosition, Quaternion.identity);
            }
            else
            {
                /*
                //     1. target block will be chosen by llm 
                //     2. llm will be given position of all blocks and will choose target block gameobject
                //     3. llm will choose destination position
                //     4. with target block gameobject and destination, assign the variables 
                //     */


                //     //BlockAgent blockAgent = agent.GetComponent<BlockAgent>();

                //     //blockAgent.targetBlock = ;
                    
                //     //Vector3 destinationPosition = ;
                //     //blockAgent.targetBlockDestinationPos = ;
                //     //Instantiate(destinationPrefab, destinationPosition, Quaternion.identity);    
            }
        }
    }

    // create objects if not, else randomize all environment object positions and reassign vars
    public void ResetBlockArea()
    { 
        //create n blocks per agent only once
        if (!blocksCreated)
        {
            CreateObjects(blocksPerAgent);
            blocksCreated = true;
        }
        else// else randomize position of existing objects and reassign vars to agent
        {
            foreach (GameObject agent in agents)
            {
                
                BlockAgent blockAgent = agent.GetComponent<BlockAgent>();
                
                //move agents randomly
                if (agent.transform.parent == gameObject.transform)
                {
                    agent.transform.position = new Vector3(Random.Range(-range, range), 2f,
                        Random.Range(-range, range))
                        + transform.position;
                    agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
                }
                
                //move blocks randomly and reassign vars to agent
                foreach (GameObject block in blockAgent.spawnedBlocksPerAgent)
                {
                    if (block == null) continue; 

                    Vector3 randomBlockPos = new Vector3(
                        Random.Range(-range, range), 
                        1f, 
                        Random.Range(-range, range)
                    ) + transform.position;

                    block.transform.position = randomBlockPos;
                    block.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 90f);

                    //reassign targetBlockPos var to agent
                    Vector3 randomizedTargetBlockPos = blockAgent.targetBlock.transform.position;
                    blockAgent.targetBlockPos = new Vector2(randomizedTargetBlockPos.x, randomizedTargetBlockPos.z);

                }

                // 4. If training, randomize destinationPos and reassign var to agent
                if (isTraining)
                {
                    Vector3 randomDestinationPosition = new Vector3(
                        Random.Range(-range, range),
                        3f,
                        Random.Range(-range, range)
                    ) + transform.position;
                    
                    blockAgent.destinationObject.transform.position = randomDestinationPosition;
                    blockAgent.targetBlockDestinationPos = new Vector2(randomDestinationPosition.x, randomDestinationPosition.z);
                    
                }
            }
        }
        
    }

    // with llm integration, this will be called when an llm chooses a target block
    public void SetBlockMaterial(GameObject block, Material material)
    {
        block.GetComponent<Renderer>().material = material;
    }
}
