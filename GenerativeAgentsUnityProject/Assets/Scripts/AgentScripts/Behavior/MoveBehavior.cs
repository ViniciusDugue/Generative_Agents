using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Vector3 target;
    private BehaviorManager bm;
    private bool promptLLM = false;

    protected override void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        bm      = GetComponent<BehaviorManager>();
        target  = transform.position;

        // make sure every agent gets a unique NavMesh avoidance priority
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority      = Random.Range(0, 100);
        agent.autoBraking            = false;
    }

    public void setTarget(Vector3 newPosition)
    {
        // How far around the home should agents spread?
        const float jitterRadius = 6f;

        // Pick a random offset in XZ
        Vector2 rnd = Random.insideUnitCircle * jitterRadius;
        Vector3 jittered = new Vector3(
            newPosition.x + rnd.x,
            newPosition.y,
            newPosition.z + rnd.y
        );

        // (Optionally) snap to the NavMesh so it’s always a valid point:
        NavMeshHit hit;
        if (NavMesh.SamplePosition(jittered, out hit, jitterRadius, NavMesh.AllAreas))
            jittered = hit.position;

        // Store & send to the agent
        target = jittered;
        agent.SetDestination(jittered);

        Debug.Log($"[Agent {bm.agentID}] Move→ {jittered} (home + jitter)");
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
        agent.isStopped = false;

        if (agent != null && target != null)
        {
            agent.SetDestination(target);
        }

        if(agent.remainingDistance > agent.stoppingDistance)
            promptLLM = false; 

        // once we’ve arrived (within stoppingDistance), fire exactly one update:
        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            !promptLLM)
        {
            // bm.UpdateLLM = true;
            // bm.MapDataExist = true;
            // promptLLM = true;
        }
        
    }
}