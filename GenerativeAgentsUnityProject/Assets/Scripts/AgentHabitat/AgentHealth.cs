using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AgentHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Health Bar UI")]
    public Slider slider;
    public Gradient gradient;
    public Image fill;

    private bool canTakeDamage;

    void Start()
    {
        // Initialize health values.
        currentHealth = maxHealth;
        if(slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
            fill.color = gradient.Evaluate(1f);
        }
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

    private IEnumerator HitTimer(float timer)
    {
        canTakeDamage = false; // Disable damage
        yield return new WaitForSeconds(timer); // Wait for the duration
        canTakeDamage = true; // Re-enable damage
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if(slider != null)
        {
            slider.value = currentHealth;
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        gameObject.SetActive(false);
    }
}
