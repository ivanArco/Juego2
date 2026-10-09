using UnityEngine;

public class ClassBala : MonoBehaviour
{
    public float velocidadDisparo = 16f;
    void Start()
    {
        this.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(0, this.velocidadDisparo, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y > 10.5f)
        {
            Destroy(gameObject);
            print("Disparo destruido...");
        }
    }
    void OnTriggerEnter2D(Collider2D collider)

    {

        Destroy (collider.gameObject);

    }
}
