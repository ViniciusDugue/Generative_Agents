using System;
using System.Text;
using UnityEngine;

public class MapEncoder : MonoBehaviour
{
    public Camera mapCamera; // Assign the 2DMapCamera in Inspector
    public int mapWidth = 50;
    public int mapHeight = 50;
    public KeyCode captureMapKey = KeyCode.E;
    public KeyCode captureAgentInfoKey = KeyCode.A;
    private Texture2D capturedTexture;

    private void Update()
    {
        if (Input.GetKeyDown(captureMapKey))
        {
            string base64Map = CaptureAndEncodeMap();
            if (!string.IsNullOrEmpty(base64Map))
            {
                Debug.Log($"Map Base64: {base64Map.Substring(0, 100)}..."); // Preview first 100 characters
            }
        }

        if (Input.GetKeyDown(captureAgentInfoKey))
        {
            CaptureAndSendAgentInfo();
        }
    }

    public string CaptureAndEncodeMap()
    {
        if (mapCamera == null)
        {
            Debug.LogError("MapEncoder: No mapCamera assigned.");
            return null;
        }

        // Capture map without changing camera view
        RenderTexture tempRT = new RenderTexture(mapWidth, mapHeight, 24);
        RenderTexture previousRT = mapCamera.targetTexture;
        RenderTexture currentRT = RenderTexture.active;

        mapCamera.targetTexture = tempRT;
        mapCamera.Render();

        RenderTexture.active = tempRT;
        capturedTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
        capturedTexture.ReadPixels(new Rect(0, 0, mapWidth, mapHeight), 0, 0);
        capturedTexture.Apply();

        RenderTexture.active = currentRT; // Restore original
        mapCamera.targetTexture = previousRT; // Restore camera target texture
        tempRT.Release();
        Destroy(tempRT);

        byte[] pngData = capturedTexture.EncodeToPNG();
        string base64String = Convert.ToBase64String(pngData);

        Debug.Log("Map captured and encoded to Base64.");
        return base64String;
    }

    private void CaptureAndSendAgentInfo()
    {
        Debug.Log("Agent information captured and sent.");
        // Placeholder for capturing and sending agent information.
        // Replace with actual agent data logic when needed.
    }
}
