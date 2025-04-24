using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int health = 30; 

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        Destroy(gameObject);
    }
}