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

        // ✅ Get Terrain Dimensions
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            mapWidth = terrain.terrainData.size.x;
            mapHeight = terrain.terrainData.size.z;
            Debug.Log($"✅ Map Width: {mapWidth}, Map Height: {mapHeight}");
        }
        else
        {
            Debug.LogWarning("⚠️ Terrain not found. Using default map width and height.");
        }

        // ✅ Assign Agent Reference
        if (agent == null)
        {
            agent = GameObject.FindWithTag("agent");
            if (agent == null)
            {
                Debug.LogError("❌ Agent not found. Ensure the agent has the correct tag.");
            }
        }

        // ✅ Send map data periodically
        InvokeRepeating(nameof(SendMapDataToBackend), 1f, updateInterval);
    }

    private async void SendMapDataToBackend()
    {
        if (agent == null)
        {
            Debug.LogError("Agent not found. Ensure the agent has the correct tag.");
            return;
        }

        // ✅ Collect game objects dynamically
        var objects = new List<object>();
        CollectObjects("agent", objects);
        CollectObjects("enemyAgent", objects);
        CollectObjects("food", objects);

        // ✅ Get agent state dynamically from the agent object
        Vector3 agentPosition = agent.transform.position;

        // 🔥 Dynamic health, exhaustion, and status obtained via component access
        int agentHealth = GetAgentAttribute(agent, "Health");
        int agentExhaustion = GetAgentAttribute(agent, "Exhaustion");
        string agentStatus = GetAgentStatus(agent);

        // ✅ Create JSON payload
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

        // ✅ Send to backend
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

            // ✅ Get dynamic attributes for each agent
            int health = obj.CompareTag("agent") ? GetAgentAttribute(obj, "Health") : 0;
            int exhaustion = obj.CompareTag("agent") ? GetAgentAttribute(obj, "Exhaustion") : 0;
            string status = obj.CompareTag("agent") ? GetAgentStatus(obj) : "N/A";

            list.Add(new
            {
                type = tag,
                id = obj.GetInstanceID(),
                position = new { x = pos.x, y = pos.z },
                health = health,
                exhaustion = exhaustion,
                status = status
            });
        }
    }

    private int GetAgentAttribute(GameObject obj, string attributeName)
    {
        // ✅ Dynamically access the agent's attributes
        var property = obj.GetType().GetProperty(attributeName);
        if (property != null)
        {
            return (int)property.GetValue(obj);
        }
        else
        {
            Debug.LogWarning($"⚠️ Attribute '{attributeName}' not found on {obj.name}");
            return 0;
        }
    }

    private string GetAgentStatus(GameObject obj)
    {
        var property = obj.GetType().GetProperty("Status");
        if (property != null)
        {
            return property.GetValue(obj)?.ToString() ?? "unknown";
        }
        else
        {
            Debug.LogWarning($"⚠️ Status property not found on {obj.name}");
            return "unknown";
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
            case "avoidenemy":
                Debug.Log("⚔️ Agent is avoiding enemy.");
                break;
            case "explore":
                Debug.Log("🌍 Agent is exploring.");
                break;
            default:
                Debug.Log($"🤔 Unknown action: {action}");
                break;
        }
    }
}
