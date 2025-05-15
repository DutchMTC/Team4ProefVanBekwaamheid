using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int health = 30;
    private int _initialHealth;

    private void Awake()
    {
        _initialHealth = health;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    public void ReceiveHeal(int amount)
    {
        health = Mathf.Min(health + amount, _initialHealth);
        Debug.Log(gameObject.name + " received " + amount + " health. Current health: " + health);
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        Destroy(gameObject);
    }
}