using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HungerDamage : MonoBehaviour
{
    [Header("Starvation Settings")]
    [Tooltip("Below this hunger value, the agent is considered starving.")]
    public int starvationThreshold = 20;
    
    [Tooltip("Damage to apply per tick when starving.")]
    public int starvationDamage = 5;
    
    [Tooltip("Interval in seconds between each damage tick.")]
    public float damageInterval = 3f;
    
    private AgentHeal agentHeal;
    private AgentHealth agentHealth;
    private bool isStarving = false;
    private Coroutine starvationCoroutine;

    void Start()
    {
        // Assume AgentHeal holds currentHunger and maxHunger.
        agentHeal = GetComponent<AgentHeal>();
        agentHealth = GetComponent<AgentHealth>();

        if (agentHeal == null || agentHealth == null)
        {
            Debug.LogError("HungerDamage requires both AgentHeal and AgentHealth components on " + gameObject.name);
        }
    }

    void Update()
    {
        // If the agent's hunger is below the threshold, begin applying starvation damage.
        if (agentHeal.currentHunger < starvationThreshold && !isStarving)
        {
            isStarving = true;
            starvationCoroutine = StartCoroutine(ApplyStarvationDamage());
        }
        // If the agent's hunger has recovered, stop applying damage.
        else if (agentHeal.currentHunger >= starvationThreshold && isStarving)
        {
            if (starvationCoroutine != null)
            {
                StopCoroutine(starvationCoroutine);
                starvationCoroutine = null;
            }
            isStarving = false;
        }
    }

    private IEnumerator ApplyStarvationDamage()
    {
        while (agentHeal.currentHunger < starvationThreshold)
        {
            Debug.Log($"{gameObject.name} is starving. Taking {starvationDamage} damage.");
            agentHealth.TakeDamage(starvationDamage);
            yield return new WaitForSeconds(damageInterval);
        }
        isStarving = false;
    }
}