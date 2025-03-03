using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentHeal : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;
    private bool canTakeDamage;

    [Header("Hunger & Fitness Settings")]
    [Tooltip("Hunger value (100 means fully satiated; 0 means starving).")]
    public int hunger = 100;
    public int maxHunger = 100;
    [Tooltip("Fitness score used to prioritize food dispensing.")]
    public float fitnessScore = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        canTakeDamage = true;
    }

    // Example collision logic for taking damage from enemy agents.
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("enemyAgent") && canTakeDamage)
        {
            TakeDamage(25);
            Debug.Log("We hit an enemy. We took damage.");
            Debug.Log("Current health: " + currentHealth);
            StartCoroutine(HitTimer(1));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitTimer(float timer)
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(timer);
        canTakeDamage = true;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        gameObject.SetActive(false);
    }

    // This method is called by the Habitat when food is dispensed.
    // The agent will heal if injured, or eat if hungry.
    public void ReceiveFood(int foodValue)
    {
        if (currentHealth < maxHealth)
        {
            Heal(foodValue);
        }
        else if (hunger < maxHunger)
        {
            Eat(foodValue);
        }
        else
        {
            Debug.Log(name + " is fully healthy and satiated.");
        }
    }

    // Increase health, update the health bar.
    public void Heal(int amount)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        healthBar.SetHealth(currentHealth);
        Debug.Log(name + " healed from " + oldHealth + " to " + currentHealth);
    }

    // Increase hunger value.
    public void Eat(int amount)
    {
        int oldHunger = hunger;
        hunger = Mathf.Min(hunger + amount, maxHunger);
        Debug.Log(name + " increased hunger from " + oldHunger + " to " + hunger);
    }

    // Optional: methods to move to or from the habitat.
    public void GoToHabitat(Habitat habitat)
    {
        // Add movement logic here (e.g., using a NavMeshAgent)
        habitat.RegisterAgent(this);
    }

    public void LeaveHabitat(Habitat habitat)
    {
        habitat.UnregisterAgent(this);
    }
}
