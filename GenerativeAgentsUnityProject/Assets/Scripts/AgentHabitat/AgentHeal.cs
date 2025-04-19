using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AgentHeal : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Hunger Settings")]
    [Tooltip("The maximum hunger value. When hunger is full, healing begins.")]
    public int maxHunger = 100;
    [Tooltip("Current hunger level. Agents start out hungry.")]
    public int currentHunger = 0;
    [Tooltip("Amount of food needed to fill hunger (per portion).")]
    public int foodPortionValue = 10;

    [Header("Healing Settings")]
    [Tooltip("Health restored per healing tick once hunger is full.")]
    public int healingPerTick = 5;
    [Tooltip("Time (in seconds) between healing ticks.")]
    public float healingInterval = 1f;

    [Header("Hunger Damage Settings")]
    [Tooltip("Damage taken when hunger is at or below the threshold each decay tick.")]
    public int hungerDamage = 5;
    [Tooltip("When hunger is at or below this value, the agent takes damage.")]
    public int hungerDamageThreshold = 30;
    [Tooltip("Hunger points decreased per decay tick.")]
    public int hungerDecay = 10;
    [Tooltip("Time in seconds between hunger decay ticks.")]
    public float hungerDecayInterval = 10f;

    [Header("UI (Optional)")]
    public HealthBar healthBar;

    private bool isHealing = false;

    void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(maxHealth);
        }

        // Start the hunger decay routine.
        StartCoroutine(HungerDecayRoutine());
    }

    /// <summary>
    /// Called by the Habitat when the agent enters the habitat at night.
    /// </summary>
    public void ReceiveFood(int portion)
    {
        // Increase hunger based on the food portion dispensed.
        currentHunger += portion;
        if (currentHunger > maxHunger)
        {
            currentHunger = maxHunger;
        }
        // Debug.Log($"{gameObject.name} received food. Hunger: {currentHunger}/{maxHunger}");

        // Once hunger is full, start the healing process.
        if (currentHunger >= maxHunger && !isHealing)
        {
            StartCoroutine(HealOverTime());
        }
    }

    /// <summary>
    /// Gradually heals the agent as long as hunger is full.
    /// </summary>
    private IEnumerator HealOverTime()
    {
        isHealing = true;
        // Heal continuously every healingInterval until health is full.
        while (currentHealth < maxHealth && currentHunger >= maxHunger)
        {
            currentHealth += healingPerTick;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }
            // Debug.Log($"{gameObject.name} healed to {currentHealth}/{maxHealth}");
            yield return new WaitForSeconds(healingInterval);
        }
        isHealing = false;
    }

    /// <summary>
    /// Every hungerDecayInterval seconds, reduce hunger by hungerDecay.
    /// If hunger falls at or below hungerDamageThreshold, apply starvation damage.
    /// </summary>
    private IEnumerator HungerDecayRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(hungerDecayInterval);
            currentHunger -= hungerDecay;
            if (currentHunger < 0)
                currentHunger = 0;
            // Debug.Log($"{gameObject.name} hunger decayed: {currentHunger}/{maxHunger}");
            if (currentHunger <= hungerDamageThreshold)
            {
                AgentHealth agentHealth = GetComponent<AgentHealth>();
                if (agentHealth != null)
                {
                    agentHealth.TakeDamage(hungerDamage);
                    // Debug.Log($"{gameObject.name} takes {hungerDamage} damage due to starvation.");
                }
            }
        }
    }

    /// <summary>
    /// Optional: Called if the agent takes damage (e.g., from enemy collisions).
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        gameObject.SetActive(false);
    }
}
