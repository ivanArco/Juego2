using UnityEngine;

public class RangoEnemigo : MonoBehaviour
{

    public Animator ani;
    public EnemyAI enemigo;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ani.SetBool("walk", false);
            ani.SetBool("run", false);
            ani.SetBool("attack", true);
            enemigo.atacando = true;
            GetComponent<BoxCollider2D>().enabled = false;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
