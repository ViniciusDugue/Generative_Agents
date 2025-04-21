using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AgentMapManager : MonoBehaviour
{

    public float detectionRange = 1000f; // Agent's personal sensor range.


    public List<MarkerData> knownMarkers = new List<MarkerData>();

    // Static list to hold all agentInfo instances.
    public static List<AgentMapInfo> allAgentMapInfos = new List<AgentMapInfo>();
    private Transform initialAgentMapPosition
    public 

    IEnumerator registerAgents() {
        // Wait for Agents to spawn in and 
        yield return new WaitForSeconds(0.5f);

        // Register all Personal Maps for each Agent
        GameObject[] agentList;
        agentList = GameObject.FindGameObjectsWithTag("Agent");
        if (agentList == null) 
        {
            Debug.Log("No Agents Found"); 
            yield break;
        }
        
        foreach(GameObject agent in agentList) {
            if (agent.GetComponent<AgentMapInfo>() != null) 
            {
                Debug.Log("Adding Personal Map for  + agent.name");
                allAgentMapInfos.Add(agent.GetComponent<AgentMapInfo>());
            }
        }
    }

    // Structure to store marker data for each discovered marker.
    public struct MarkerData
    {
        public MarkerEventManager.MarkerType markerType;

        public Vector3 position;

        public GameObject discoveredObject; // Store the object reference

        public MarkerData(GameObject obj, MarkerEventManager.MarkerType type)
        {
            discoveredObject = obj;
            markerType = type;
            position = obj.transform.position;
        }
    }
}
