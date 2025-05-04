using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Vector3 target;

    void Awake()
    {
        target = this.gameObject.transform.position;
        agent = GetComponent<NavMeshAgent>();
    }

    public void setTarget(Vector3 newPosition) {
        target = newPosition;
        Debug.Log($"MoveBehavior target set to: {target}");
    }

    void OnEnable()
    {

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    void OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        agent.isStopped = false;
        if (agent != null && target != null)
        {
            agent.SetDestination(target);
        }
    }
}