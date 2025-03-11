using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Vector3 target;

    void Start()
    {
        target = this.gameObject.transform.position;
        agent = GetComponent<NavMeshAgent>();
    }

    protected override void OnEnable()
    {

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    protected override void OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (agent != null && target != null)
        {
            agent.SetDestination(target);
        }
    }
}