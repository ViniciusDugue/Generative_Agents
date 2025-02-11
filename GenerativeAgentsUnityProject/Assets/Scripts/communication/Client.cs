using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;


public class Client : MonoBehaviour
{
    public TextMeshProUGUI  displayText;
    public TMP_InputField inputField;  // Reference to a TextMeshPro Input Field
    public GameObject agent;
    private HttpClient client;


    void Start()
    {
        client = new HttpClient();

        // Add a listener to the TMP_InputField to send data on value change
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldValueChanged);
        }

        if (agent == null)
        {
            agent = GameObject.FindWithTag("agent");
        }

        // now we send the agent data to backend evert 5 seconds
        InvokeRepeating(nameof(SendAgentData), 1f, 5f);
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
            var jsonData = new { agent_id = 1, health = 100, status = "active", position = new { x = 0, y = 0, z = 0 } };
            string jsonString = JsonConvert.SerializeObject(jsonData);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

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
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.ToString());
        }
    }

    async void SendAgentData()
    {
        if (agent == null)
        {
            Debug.LogError("No agent assigned for communication.");
            return;
        }

        // Get agent position
        Vector3 position = agent.transform.position;

        // Create agent JSON data
        var agentData = new
        {
            agent_id = 1,  // Modify dynamically if needed
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
                    PerformAction(nextAction); // Perform the received action
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

    void PerformAction(string action)
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
