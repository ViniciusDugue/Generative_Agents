using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class MapEncoder : MonoBehaviour
{
    private HttpClient client;
    private string apiUrl = "http://127.0.0.1:12345/nlp";

    [Header("Map Settings")]
    public float mapWidth = 50f;
    public float mapHeight = 50f;
    public float updateInterval = 5f; // Send data every 5 seconds

    [Header("Agent Reference")]
    public GameObject agent;

    private void Start()
    {
        client = new HttpClient();

        if (agent == null)
        {
            agent = GameObject.FindWithTag("agent");
        }

        // Send map data periodically
        InvokeRepeating(nameof(SendMapDataToBackend), 1f, updateInterval);
    }

    private async void SendMapDataToBackend()
    {
        if (agent == null)
        {
            Debug.LogError("Agent not found. Ensure the agent has the correct tag.");
            return;
        }

        // Collect game objects
        var objects = new List<object>();
        CollectObjects("agent", objects);
        CollectObjects("enemyAgent", objects);
        CollectObjects("food", objects);

        // Get agent state
        Vector3 agentPosition = agent.transform.position;
        int agentHealth = 100; // Replace with actual health logic
        int agentExhaustion = 20; // Replace with actual exhaustion logic
        string agentStatus = "active"; // Replace with actual status logic

        // Create JSON payload
        var payload = new
        {
            map = new
            {
                width = mapWidth,
                height = mapHeight,
                objects = objects
            },
            agent_state = new
            {
                agent_id = agent.GetInstanceID(),
                health = agentHealth,
                exhaustion = agentExhaustion,
                status = agentStatus,
                position = new { x = agentPosition.x, y = agentPosition.z }
            }
        };

        string jsonString = JsonConvert.SerializeObject(payload, Formatting.Indented);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        // Debug print JSON
        Debug.Log($"📤 Sending JSON to LLM: {jsonString}");

        // Send to backend
        try
        {
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                Debug.Log("✅ Response from LLM: " + responseData);

                // Process LLM response
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseData);
                if (responseJson != null && responseJson.ContainsKey("next_action"))
                {
                    string nextAction = responseJson["next_action"].ToString();
                    PerformAction(nextAction);
                }
            }
            else
            {
                Debug.LogError("❌ Error: " + response.StatusCode);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("⚠️ Exception: " + e.ToString());
        }
    }

    private void CollectObjects(string tag, List<object> list)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Vector3 pos = obj.transform.position;
            list.Add(new
            {
                type = tag,
                id = obj.GetInstanceID(),
                position = new { x = pos.x, y = pos.z }
            });
        }
    }

    private void PerformAction(string action)
    {
        action = action.Trim().ToLower();
        switch (action)
        {
            case "foodgatheragent":
                Debug.Log("🍎 Agent is gathering food.");
                break;
            case "restbehavior":
                Debug.Log("💤 Agent is resting.");
                break;
            default:
                Debug.Log($"🤔 Unknown action: {action}");
                break;
        }
    }
}
