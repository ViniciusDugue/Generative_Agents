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

    [Header("UI (Optional)")]
    public HealthBar healthBar;

    private bool isHealing = false;

    void Start()
    {
        currentHealth = maxHealth;
        // Assume agent starts hungry; you might later implement hunger decay during the day.
        currentHunger = 0;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(maxHealth);
        }
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
        Debug.Log($"{gameObject.name} received food. Hunger: {currentHunger}/{maxHunger}");

        // Once hunger is full, start the healing process.
        if (currentHunger >= maxHunger && !isHealing)
        {
            StartCoroutine(HealOverTime());
        }
    }

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
            Debug.Log($"{gameObject.name} healed to {currentHealth}/{maxHealth}");
            yield return new WaitForSeconds(healingInterval);
        }
        isHealing = false;
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
