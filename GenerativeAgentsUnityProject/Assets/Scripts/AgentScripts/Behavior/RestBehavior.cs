using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class RestBehavior : AgentBehavior
{
    private GameObject agentObject;
    Rigidbody m_Rigidbody;
    public Vector3 rotationSpeed = new Vector3(0, 100, 0); // Rotation speed in degrees per second

    // Start is called before the first frame update
    void Start() {
        exhaustionRate = -10f; // Rate at which the agent recovers
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}

