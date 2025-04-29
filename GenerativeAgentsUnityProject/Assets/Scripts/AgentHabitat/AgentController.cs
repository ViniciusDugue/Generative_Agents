using System.Collections;
using System.Collections.Generic;
<<<<<<< HEAD
using UnityEngine;
=======
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
>>>>>>> main

public class AgentController : MonoBehaviour
{
    public float moveSpeed = 5f;  // Adjust speed in Inspector
    private Rigidbody rb;

    // Static list that holds references to all AgentController instances.
    public static List<AgentController> allAgents = new List<AgentController>();

    // Static variable tracking the currently controlled agent's index.
    public static int selectedAgentIndex = 0;

    private void Awake()
    {
        // Register this agent.
        allAgents.Add(this);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody exists.
        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing from the agent!");
        }
    }

    void Update()
    {
        CheckForAgentSwitch();

        // Process movement input only if this agent is the selected one.
        if (allAgents[selectedAgentIndex] == this)
        {
            MoveAgent();
        }
    }

    // Checks if the user has pressed a number key (1 to 9) to switch agent control.
    void CheckForAgentSwitch()
    {
<<<<<<< HEAD
        // We'll check for keys 1 through 9.
        for (int i = 0; i < 9; i++)
        {
            // KeyCode.Alpha1 corresponds to the "1" key.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int agentIndex = i; // "1" will map to index 0, "2" to index 1, etc.
                if (agentIndex < allAgents.Count)
                {
                    selectedAgentIndex = agentIndex;
                    Debug.Log("Switched control to Agent " + (agentIndex + 1));
                }
                else
                {
                    Debug.Log("Agent " + (agentIndex + 1) + " not available.");
=======
        // Listen for keys "1" through "9"
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int desiredID = i + 1; // "1" ⇒ ID 1, "2" ⇒ ID 2, etc.

                // Safely look up the agent with that agentID
                AgentController target = allAgents.FirstOrDefault(a =>
                {
                    var bm = a.GetComponent<BehaviorManager>();
                    return bm != null && bm.agentID == desiredID;
                });

                if (target != null)
                {
                    selectedAgentIndex = allAgents.IndexOf(target);
                    Debug.Log($"Switched control to Agent {desiredID}");
                }
                else
                {
                    Debug.Log($"Agent {desiredID} not available.");
>>>>>>> main
                }
            }
        }
    }

    // Moves this agent based on horizontal and vertical input.
    void MoveAgent()
    {
        float moveX = Input.GetAxis("Horizontal"); // Typically mapped to A/D or arrow keys.
        float moveZ = Input.GetAxis("Vertical");   // Typically mapped to W/S or arrow keys.
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(moveDirection, Space.World);
    }

    private void OnDestroy()
    {
        // Remove this agent from the static list when destroyed.
        if (allAgents.Contains(this))
        {
            allAgents.Remove(this);
        }
    }

<<<<<<< HEAD
}
=======
}
>>>>>>> main
