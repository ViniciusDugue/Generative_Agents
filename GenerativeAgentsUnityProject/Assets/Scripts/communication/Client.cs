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


public class Client : MonoBehaviour
{
    public TextMeshProUGUI  displayText;
    public TMP_InputField inputField;  // Reference to a TextMeshPro Input Field
    private Dictionary<int, GameObject> agentDict = new Dictionary<int, GameObject>();
    private HttpClient client;


    void Start()
    {
        // Add a listener to the TMP_InputField to send data on value change
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldValueChanged);
        }

        // Find all active agents in the scene
        GameObject[]activeAgents = GameObject.FindGameObjectsWithTag("agent");
        if (activeAgents == null)
        {
            Debug.LogError("No active agents found.");
        }

        // Assign agent IDs to each agent for the Dictionary
        foreach(GameObject agent in activeAgents) {
            BehaviorManager bm = agent.GetComponent<BehaviorManager>();
            if (bm == null) 
            {
                Debug.LogError("No BehaviorManager found on agent.");
            }
            int agentID = agent.GetComponent<BehaviorManager>().agentID;
            agentDict.Add(agentID, agent);

            // Subscribe to the OnUpdateLLM event
            bm.OnUpdateLLM += HandleOnUpdateLLMChanged;
        }
    }

    // Handles when an agent's OnUpdateLLM is set to true
    private async void HandleOnUpdateLLMChanged(int agentID)    
    {
        Debug.Log($"🔄 OnUpdateLLM changed to TRUE for Agent {agentID}, sending data...");

        await SendAgentData(agentID); // Wait for data to be sent

        // Reset _updateLLM to false
        if (agentDict.ContainsKey(agentID))
        {
            BehaviorManager bm = agentDict[agentID].GetComponent<BehaviorManager>();
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
            agent_id = agent.GetComponent<BehaviorManager>().agentID,  
            health = 100,  // Placeholder, replace with actual health
            status = "active",
            next_action = "explore",  // Default action
            position = new { x = position.x, y = position.y, z = position.z }
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
                    Debug.Log($"Next action for Agent: {nextAction}");
                    PerformAction(agent, nextAction); // Perform the received action
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
