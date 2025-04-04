using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Vector3 target;

    override protected void Awake()
    {
        target = this.gameObject.transform.position;
        agent = GetComponent<NavMeshAgent>();
    }

    public void setTarget(Vector3 newPosition) {
        target = newPosition;
        Debug.Log($"MoveBehavior target set to: {target}");
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