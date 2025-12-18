using UnityEngine;

public class ShipExplosion : MonoBehaviour
{
    public GameObject explosionEffect;

    void Start()
    {
        gameObject.tag = "Ship";
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Drone"))
        {
            Debug.Log($"?? ДРОН КОСНУЛСЯ {gameObject.name}!");
            ExplodeShip();
        }
    }

    public void ExplodeShip()
    {
        if (explosionEffect != null)
        {
            // Создаем эффект и сразу меняем scale на 50,50,50
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            explosion.transform.localScale = new Vector3(50f, 50f, 50f);
        }

        Destroy(gameObject);
        Debug.Log($"??? {gameObject.name} УНИЧТОЖЕН!");
    }
}