using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentHealth : MonoBehaviour
{

    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;
    private bool canTakeDamage;

    // Initialize Health Values
    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        canTakeDamage = true;
    }

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

    private IEnumerator HitTimer(float timer) {
        canTakeDamage = false; // Disable damage
        yield return new WaitForSeconds(timer); // Wait for the duration
        canTakeDamage = true; // Re-enable damage
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;

        healthBar.SetHealth(currentHealth);
    }


    void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        gameObject.SetActive(false);
    }
}