using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;


public class Client : MonoBehaviour
{
    // Server address and port
    string serverAddress = "127.0.0.1";
    int serverPort = 12345;
    public TextMeshProUGUI  displayText;
    public TMP_InputField inputField;  // Reference to a TextMeshPro Input Field

    TcpClient tcpClient;

    void Start()
    {
        ConnectToServer();

        // Add a listener to the TMP_InputField to send data on value change
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldValueChanged);
        }
    }

    void Update()
    {
        string serverData = GetServerData();
        if (!string.IsNullOrEmpty(serverData))
        {
            Debug.Log("Received: " + serverData);
            displayText.text = serverData;
        }
    }

    string GetServerData()
    {
        try
        {
            if (tcpClient != null && tcpClient.Available > 0)
            {
                NetworkStream stream = tcpClient.GetStream();
                byte[] data = new byte[tcpClient.Available];
                int bytesRead = stream.Read(data, 0, data.Length);
                string responseData = Encoding.UTF8.GetString(data, 0, bytesRead);
                return responseData;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.ToString());
        }
        return null;
    }

     // Callback for TMP_InputField value change
    void OnInputFieldValueChanged(string newValue)
    {
        Debug.Log("Input field changed");

        // Send the new value to the server
        SendDataToServer(newValue);
    }

    // Moved and renamed the method
    void SendDataToServer(string message)
    {
        try
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                NetworkStream stream = tcpClient.GetStream();
                if (stream.CanWrite)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    stream.Write(data, 0, data.Length);
                    Debug.Log("Message sent: " + message);
                }
                else
                {
                    Debug.LogError("Can't write to stream");
                }
            }
            else
            {
                Debug.LogError("TCP Client not connected");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.ToString());
        }
    }

    void OnApplicationQuit()
    {
        if (tcpClient != null)
        {
            tcpClient.Close();
            Debug.Log("Connection closed");
        }
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverAddress, serverPort);
            Debug.Log("Connected");

        }
        catch (SocketException e)
        {
            Debug.LogError("SocketException: " + e.ToString());
        }
    }
}
