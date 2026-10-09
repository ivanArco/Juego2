using UnityEngine;

public class Rana : MonoBehaviour
{
    private float speed = 5f;
    private Rigidbody2D rb;
    public float fuerzaSalto = 5f; // Fuerza del salto modificable desde el Inspector

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movementShip();
    }

    void movementShip()
    {
        transform.position += new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, 0);
        transform.position += new Vector3(0, Input.GetAxis("Vertical") * speed * Time.deltaTime, 0);

        // Detecta cuando se presiona la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Aplica una fuerza instantánea hacia arriba
            rb.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
        }

    }
}
