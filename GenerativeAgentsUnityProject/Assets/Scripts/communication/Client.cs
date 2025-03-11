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


public class Client : MonoBehaviour
{
    public TextMeshProUGUI  displayText;
    public TMP_InputField inputField;  // Reference to a TextMeshPro Input Field
    private Dictionary<int, GameObject> agentDict = new Dictionary<int, GameObject>();
    

    private HttpClient client;


    void Start()
    {
        client = new HttpClient();

        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldValueChanged);
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
                Debug.Log($"Agent {agentID} registered in Client.");
                bm.OnUpdateLLM += HandleOnUpdateLLMChanged;
            }
        }
    }


    // Handles when an agent's OnUpdateLLM is set to true
    private async void HandleOnUpdateLLMChanged(int agentID)    
    {
        Debug.Log($"🔄 OnUpdateLLM changed to TRUE for Agent {agentID}, sending data...");

        if (agentDict.ContainsKey(agentID))
        {
            GameObject agent = agentDict[agentID];
            MapEncoder mapEncoder = agent.GetComponent<MapEncoder>();

            if (mapEncoder != null)
            {
                mapEncoder.CaptureAndSendMap(agentID);
                await SendAgentData(agentID); // Wait for data to be sent
            }
            else
            {
                Debug.LogError($"MapEncoder not found on Agent {agentID}");
            }

            // Reset _updateLLM to false
            BehaviorManager bm = agent.GetComponent<BehaviorManager>();
            if (bm != null)
            {
                Debug.Log($"Resetting UpdateLLM to FALSE for Agent {agentID}");
                bm.UpdateLLM = false; // Reset the flag
            }
        }
    }

    // Callback for TMP_InputField value change
    void OnInputFieldValueChanged(string newValue)
    {
        Debug.Log("Input field changed");

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
                var jsonData = new {input_string = message };
                string jsonString = JsonConvert.SerializeObject(jsonData);

                // Create the HTTP content
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:12345/nlp", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Debug.Log("Response from API: " + responseData);
                    displayText.text = responseData;
                }
                else
                {
                    Debug.LogError("Error: " + response.StatusCode);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.ToString());
        }
    }

    async Task SendAgentData(int agentID)
    {
        if (!agentDict.ContainsKey(agentID))
        {
            Debug.LogError($"Agent ID {agentID} not found in dictionary.");
            return;
        }

        GameObject agent = agentDict[agentID];
        Vector3 position = agent.transform.position;

        // Create agent JSON data
        var agentData = new
        {
            agentID = agent.GetComponent<BehaviorManager>().agentID,  
            health = 100,  // Placeholder, replace with actual health
            exhaustion = agent.GetComponent<BehaviorManager>().exhaustion,
            currentAction = agent.GetComponent<BehaviorManager>().currentAgentBehavior.GetType().Name,  // Default action
            currentPosition = new { x = position.x, y = position.y, z = position.z },
            foodLocations = GetFoodLocationsAsList(agent.GetComponent<BehaviorManager>().foodLocations),
        };

        string jsonString = JsonConvert.SerializeObject(agentData, Formatting.Indented);
        Debug.Log($"🔍 Sending JSON: {jsonString}");
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        try
        {
            HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:12345/nlp", content);

            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                Debug.Log("Agent Response: " + responseData);

                // Parse the JSON response and determine the next action
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseData);
                if (responseJson != null && responseJson.ContainsKey("next_action"))
                {
                    string nextAction = responseJson["next_action"].ToString();
                    string reasoning = responseJson["reasoning"].ToString();
                    Debug.Log($"Next action for Agent: {nextAction}");
                    Debug.Log($"Agent Reasoning: {reasoning}");
                    
                    // Switch Agent Behavior to nextAction
                    agentDict[agentID].GetComponent<BehaviorManager>().SwitchBehavior(nextAction);
                    agentDict[agentID].GetComponent<BehaviorManager>().SetMoveTarget(responseJson["location"]);
                }
            }
            else
            {
                Debug.LogError("Error: " + response.StatusCode);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.ToString());
        }
    }

    // Method to convert the HashSet to a List
    private List<Dictionary<string, float>> GetFoodLocationsAsList(HashSet<Transform> foodLocationsHashSet)
    {
        if (foodLocationsHashSet.Count == 0)  return null;

        List<Dictionary<string, float>> positionsList = new List<Dictionary<string, float>>();
        List<Transform> foodLocationsList = new List<Transform>(foodLocationsHashSet);
        foreach (Transform foodLocation in foodLocationsList)
        {
            Vector3 position = foodLocation.position;
            Dictionary<string, float> positionDict = new Dictionary<string, float>
            {
                { "x", position.x },
                { "y", position.y },
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
                Debug.Log($"Unknown action: {action}");
                break;
        }
    }

}
