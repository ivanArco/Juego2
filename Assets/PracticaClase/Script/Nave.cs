using UnityEngine;

public class Nave : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float velocidad = 5f;
    public GameObject shooting;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Movimiento();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shootingShip();
        }

    }

    void Movimiento()
    {
        // Movimiento hacia derecha
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * velocidad * Time.deltaTime);
        }

        // Movimient o hacia la izquierda
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * velocidad * Time.deltaTime);
        }

        // Movimiento hacia arriba
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime);
        }
        //Movimiento hacia abajo
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.down * velocidad * Time.deltaTime);
        }
    }

    void shootingShip()
    {
        var naveTemporal = GameObject.Find("Ave");
        if (naveTemporal != null)
        {
            Vector3 posicionIzquierda = naveTemporal.transform.position + Vector3.up * 0.45f +
            Vector3.left * 0.3f;
            Vector3 posicionDerecha = naveTemporal.transform.position + Vector3.up * 0.45f +
            Vector3.right * 0.3f;
            Instantiate(this.shooting, posicionIzquierda, Quaternion.identity);
            Instantiate(this.shooting, posicionDerecha, Quaternion.identity);
        }
    }
}
