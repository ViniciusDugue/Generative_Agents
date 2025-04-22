using System;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Collections;
using System.Threading.Tasks;
using System.Drawing.Text;
using System.Collections.Generic;

public class MapEncoder : MonoBehaviour
{
    public Camera mapCamera;
    public int mapWidth = 200;
    public int mapHeight = 200;
    public string serverUrl = "http://127.0.0.1:12345/map";
    private Texture2D capturedTexture;
    private static readonly HttpClient httpClient = new HttpClient();
    private BehaviorManager bm;
    private int agentID;

    [System.Serializable]
    public class MapPayload
    {
        public int agent_id;
        public string map_base64;
    }

    void OnEnable()
    {
        MapMarkerManager.mapDictFullyBuilt += AssignMapCamera;
    }
    void OnDisable()
    {
        MapMarkerManager.mapDictFullyBuilt -= AssignMapCamera;
    }

    void Start() {
        bm = GetComponent<BehaviorManager>();
        agentID = bm.agentID;
    }

    public void AssignMapCamera(Dictionary<string, GameObject> agentMapDict) {
        GameObject mapObj = agentMapDict[$"AgentMap-{agentID+1}"];
        Camera camera = mapObj.transform.GetChild(0).GetComponent<Camera>();
        mapCamera = camera;
    }


    public async void CaptureAndSendMap(int agentID)
    {
        string base64Map = CaptureAndEncodeMap(); // Capture the map image as Base64
        if (!string.IsNullOrEmpty(base64Map))
        {
            await SendMapToServer(agentID, base64Map); // Await async function
        }
    }

    public string CaptureAndEncodeMap()
    {
        if (mapCamera == null)
        {
            Debug.LogError("MapEncoder: No mapCamera assigned.");
            return null;
        }
        RenderTexture originalRT = mapCamera.targetTexture;
        RenderTexture tempRT = new RenderTexture(mapWidth, mapHeight, 24);
        mapCamera.targetTexture = tempRT;
        mapCamera.Render();

        RenderTexture.active = tempRT;
        capturedTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
        capturedTexture.ReadPixels(new Rect(0, 0, mapWidth, mapHeight), 0, 0);
        capturedTexture.Apply();

        RenderTexture.active = null;
        mapCamera.targetTexture = originalRT;
        Destroy(tempRT);

        byte[] pngData = capturedTexture.EncodeToPNG();
        return Convert.ToBase64String(pngData);
    }

    private async Task SendMapToServer(int agentID, string base64Map)
    {
        try
        {
            MapPayload payload = new MapPayload { agent_id = agentID, map_base64 = base64Map };
            string jsonPayload = JsonUtility.ToJson(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(serverUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Debug.Log($"Map sent successfully by Agent {agentID}.");
            }
            else
            {
                Debug.LogError($"Failed to send map by Agent {agentID}: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while sending map by Agent {agentID}: {e.Message}");
        }
    }

}
