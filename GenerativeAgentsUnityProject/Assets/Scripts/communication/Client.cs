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


public class Client : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public TMP_InputField inputField;  // Reference to a TextMeshPro Input Field
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

    // Callback for TMP_InputField value change
    void OnInputFieldValueChanged(string newValue)
    {
        Debug.Log("[Client] Input field changed");
        // Send the new value to the server
        SendDataToPost(newValue);
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
            agentID = agent.GetComponent<BehaviorManager>().agentID,
            health = 100,  // Placeholder, replace with actual health
            enemyCurrentlyDetected = agent.GetComponent<BehaviorManager>().enemyCurrentlyDetected,
            exhaustion = agent.GetComponent<BehaviorManager>().exhaustion,
            currentAction = agent.GetComponent<BehaviorManager>().currentAgentBehavior.GetType().Name,  // Default action
            currentPosition = new { x = position.x, z = position.z },
            foodLocations = GetFoodLocationsAsList(agent.GetComponent<BehaviorManager>().foodLocations),
            mapData = mapData,
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
                Debug.Log($"[Client] LLM returned next_action: {nextAction}");
                Debug.Log($"[Client] Agent Reasoning: {reasoning}");

                // Attempt to switch Agent Behavior to nextAction
                Debug.Log($"[Client] Attempting to switch behavior for Agent {agentID} to {nextAction}");
                agentDict[agentID].GetComponent<BehaviorManager>().SwitchBehavior(nextAction);

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

    void PerformAction(GameObject agent, string action)
    {
        action = action.Trim();

        switch (action)
        {
            case "explore":
                agent.transform.position += new Vector3(1, 0, 0); // Move right
                break;
            case "repair":
                Debug.Log("Agent is repairing.");
                break;
            case "return_to_base":
                agent.transform.position = Vector3.zero; // Reset position
                break;
            default:
                Debug.Log($"[Client] Unknown action: {action}");
                break;
        }
    }
}
