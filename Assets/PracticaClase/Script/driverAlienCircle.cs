using UnityEngine;

public class diverAlienCircle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float speed = -4f;
    private int margen = 1;
    private float positionX;
    private float positionY;

    void Start()
    {
        positionX = Random.Range(-9f, 9f);
        positionY = 5f;
        this.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(speed,
        speed, 0);
        transform.position = new Vector3(positionX, positionY,
        transform.position.z);
    }
    void Update()
    {
        // Exit for the left side
        if (transform.position.x <= -10f)
        {
            positionX = Random.Range(-9f, 9f);
            positionY = 5f;
            positionY = Mathf.Clamp(transform.position.y, 5.5f + margen, -5.5f -
           margen);

            this.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(speed,
            speed, 0);
            transform.position = new Vector3(positionX, positionY,
            transform.position.z);
        }
        // Exit for the right side
        if (transform.position.x >= 10f)
        {
            positionX = Random.Range(-9f, 9f); ;
            positionY = 5f;
            this.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(speed, speed, 0);
            positionY = Mathf.Clamp(transform.position.y, 5.5f + margen, -5.5f - margen);
            transform.position = new Vector3(positionX, positionY,
            transform.position.z);
        }
        // Exit for the bottom side
        if (transform.position.y <= -6f)
        {
            positionX = Random.Range(-9f, 9f);
            positionY = 5f;
            positionX = Mathf.Clamp(transform.position.x, -10f + margen, 10f - margen);
            this.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(speed * -1, speed, 0);
            transform.position = new Vector3(positionX, positionY, transform.position.z);
        }
    }
}
