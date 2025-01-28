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

    void Start()
    {
        // Add a listener to the TMP_InputField to send data on value change
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldValueChanged);
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
                var jsonData = new { text = message };
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
    

}
