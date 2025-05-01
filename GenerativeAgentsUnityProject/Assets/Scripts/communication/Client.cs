using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.MLAgents;
using UnityEditor.UIElements;
using System.Net;
using System;
using NUnit.Framework.Constraints;
using AgentDataStructures;

public class Client : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    private GameObject habitat;
    private Habitat habitatComponent;
    private Dictionary<int, GameObject> agentDict = new Dictionary<int, GameObject>();

    private HttpClient client;

    void Start()
    {
        client = new HttpClient();
        Debug.Log("[Client] Client script started.");  // Confirm that the client has started.

        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");
        foreach (GameObject agent in agents)
        {
            if (agent != null) 
            {
                RegisterAgent(agent);
            }
        }

        // Assign Reference
        habitat = GameObject.FindGameObjectWithTag("habitat");
        habitatComponent = habitat.GetComponent<Habitat>();

    }

    public void RegisterAgent(GameObject agent)
    {
        BehaviorManager bm = agent.GetComponent<BehaviorManager>();
        if (bm != null)
        {
            int agentID = bm.agentID;
            if (!agentDict.ContainsKey(agentID))
            {
                agentDict.Add(agentID, agent);
                Debug.Log($"[Client] Agent {agentID} registered in Client.");
                bm.OnUpdateLLM += HandleOnUpdateLLMChanged;
            }
        }
    }

    // Handles when an agent's OnUpdateLLM is set to true
    private async void HandleOnUpdateLLMChanged(int agentID, bool mapDataExist)    
    {
        Debug.Log($"[Client] 🔄 OnUpdateLLM changed to TRUE for Agent {agentID}, sending data...");

        if (agentDict.ContainsKey(agentID))
        {
            GameObject agent = agentDict[agentID];
            MapEncoder mapEncoder = agent.GetComponent<MapEncoder>();

            if (mapEncoder != null)
            {
                // mapEncoder.CaptureAndSendMap(agentID);
                await SendAgentData(agent, mapDataExist); // Wait for data to be sent
            }
            else
            {
                Debug.LogError($"[Client] MapEncoder not found on Agent {agentID}");
            }

            // Reset _updateLLM to false
            BehaviorManager bm = agent.GetComponent<BehaviorManager>();
            if (bm != null)
            {
                Debug.Log($"[Client] Resetting UpdateLLM to FALSE for Agent {agentID}");
                bm.UpdateLLM = false; // Reset the flag
            }
        }
    }

    async void SendDataToPost(string message)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Prepare the JSON payload
                var jsonData = new { input_string = message };
                string jsonString = JsonConvert.SerializeObject(jsonData);
                Debug.Log($"[Client] Sending input: {jsonString}");

                // Create the HTTP content
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:12345/nlp", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Debug.Log("[Client] Response from API: " + responseData);
                    displayText.text = responseData;
                }
                else
                {
                    Debug.LogError("[Client] Error: " + response.StatusCode);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Client] Exception: " + e.ToString());
        }
    }

    async Task SendAgentData(GameObject agent, bool mapDataExist)
    {
        MapEncoder mapEncoder = agent.GetComponent<MapEncoder>();
        BehaviorManager bm = agent.GetComponent<BehaviorManager>();
        int agentID = agent.GetComponent<BehaviorManager>().agentID;
        string mapData = null;

        // Check if the agent exists in the dictionary
        if (!agentDict.ContainsKey(agentID))
        {
            Debug.LogError($"[Client] Agent ID {agentID} not found in dictionary.");
            return;
        }

        if (mapDataExist)
        {
            mapData = mapEncoder.CaptureAndEncodeMap(); // Capture the map image as Base64
        }

        Vector3 position = agent.transform.position;

        // Create agent JSON data
        var agentData = new
        {
            agentID = bm.agentID,
            currentAction = bm.currentAgentBehavior.GetType().Name,  // Default action
            currentPosition = new { x = position.x, z = position.z },
            currentHunger = bm.CurrentHunger,
            maxFood = 3, // Placeholder, replace with actual maxFood
            currentFood = bm.getFood(),
            habitatStoredFood = habitatComponent.storedFood,
            fitness = bm.fitnessScore,
            health = 100,  // Placeholder, replace with actual health
            enemyCurrentlyDetected = bm.enemyCurrentlyDetected,
            exhaustion = bm.exhaustion,
            habitatLocation = new {x = habitatComponent.transform.position.x, z =habitatComponent.transform.position.z},
            activeFoodLocations = GetFoodLocationsAsList(bm.activeFoodLocations),
            foodLocations = GetFoodLocationsAsList(bm.foodLocations),
            mapData = mapData,
            habitatStoredBlocks = habitatComponent.storedBlocks,
            blockLocations = GetBlockLocationsAsList(bm.blockPositionsList),
        };

        string jsonString = JsonConvert.SerializeObject(agentData, Formatting.Indented);
        Debug.Log($"[Client] 🔍 Sending agent JSON: {jsonString}");
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        try
        {
            HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:12345/nlp", content);

            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                Debug.Log("[Client] Agent Response: " + responseData);

                // Parse the JSON response and determine the next action
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseData);
                if (responseJson == null)
                {
                    Debug.LogError("[Client] Failed to deserialize LLM response.");
                    return;
                }

                if (!responseJson.ContainsKey("next_action"))
                {
                    Debug.LogError("[Client] LLM response does not contain 'next_action'. Full response: " + responseData);
                    return;
                }

                string nextAction = responseJson["next_action"].ToString();
                string reasoning = responseJson.ContainsKey("reasoning") ? responseJson["reasoning"].ToString() : "No reasoning provided";
                agent.GetComponent<BehaviorManager>().reasoning = reasoning;
                Debug.Log($"[Client] LLM returned next_action: {nextAction}");
                Debug.Log($"[Client] Agent Reasoning: {reasoning}");

                // Attempt to switch Agent Behavior to nextAction
                Debug.Log($"[Client] Attempting to switch behavior for Agent {agentID} to {nextAction}");
                agentDict[agentID].GetComponent<BehaviorManager>().SwitchBehavior(nextAction);
                
                // Check if the response contains the key "eatPersonalFoodSupply" and call the method if it does
                if (responseJson.TryGetValue("eatPersonalFoodSupply", out var raw) &&
                    bool.TryParse(raw?.ToString(), out bool eatFood) &&   // accepts "True", "true", "FALSE", etc.
                    eatFood)
                {
                    agentDict[agentID].GetComponent<BehaviorManager>().eatPersonalFoodSupply();
                }

                // If a location is provided, update move target
                if (responseJson.ContainsKey("location"))
                {
                    Debug.Log($"[Client] Setting move target for Agent {agentID} using location from response.");
                    agentDict[agentID].GetComponent<BehaviorManager>().SetMoveTarget(responseJson["location"]);
                }
                else
                {
                    Debug.LogWarning("[Client] No location provided in LLM response.");
                }

                if (responseJson.ContainsKey("blockToMove"))
                {
                    Debug.Log($"[Client] Setting move target for Agent {agentID} using location from response.");
                    agentDict[agentID].GetComponent<BehaviorManager>().SetMoveBlockData(responseJson["blockToMove"].ToString());
                }
                else
                {
                    Debug.LogWarning("[Client] No location provided in LLM response.");
                }
            }
            else
            {
                Debug.LogError("[Client] Error: " + response.StatusCode);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Client] Exception: " + e.ToString());
        }
    }

    // Method to convert the HashSet to a List
    private List<Dictionary<string, float>> GetFoodLocationsAsList(HashSet<Transform> foodLocationsHashSet)
    {
        List<Dictionary<string, float>> positionsList = new List<Dictionary<string, float>>();

        if (foodLocationsHashSet.Count == 0)  return positionsList;

        List<Transform> foodLocationsList = new List<Transform>(foodLocationsHashSet);
        foreach (Transform foodLocation in foodLocationsList)
        {
            Vector3 position = foodLocation.position;
            Dictionary<string, float> positionDict = new Dictionary<string, float>
            {
                { "x", position.x },
                { "z", position.z }
            };
            positionsList.Add(positionDict);
        }
        return positionsList;
    }

    // inside your Client class, after GetFoodLocationsAsList:
    private List<Dictionary<string, Dictionary<string, float>>> GetBlockLocationsAsList(List<BlockPositionEntry> blocks)
    {
        var list = new List<Dictionary<string, Dictionary<string, float>>>();
        foreach (var entry in blocks)
        {
            // Unity's Vector2 stores x and y; we treat y as your "z" axis
            var coords = new Dictionary<string, float>
            {
                ["x"] = entry.position.x,
                ["z"] = entry.position.y
            };
            list.Add(new Dictionary<string, Dictionary<string, float>>
            {
                [entry.blockName] = coords
            });
        }
        return list;
    }

}
