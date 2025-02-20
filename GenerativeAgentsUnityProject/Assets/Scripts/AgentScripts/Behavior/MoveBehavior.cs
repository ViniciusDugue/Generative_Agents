using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Transform target;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        // foreach(Transform t in transformList) 
        // {
        //     agent.SetDestination(t.position);
        // }
        agent.SetDestination(target.position);
    }
}
