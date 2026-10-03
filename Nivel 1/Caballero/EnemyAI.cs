using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyAI : MonoBehaviour
{
    public int rutina;
    public float cronometro;
    public Animator anim;
    public int direccion;
    public float speed_walk;
    public float speed_run;
    public GameObject target;
    public bool atacando;

    public float rango_vision;
    public float rango_ataque;
    public GameObject rango;
    public GameObject Hit;
    public float tiempoEntreAtaques = 0.6f;
    private float tiempoUltimoAtaque = -99f;
    public bool muerto;
    private EnemyHealth vida;
    //weqewqe
    private void Start()
    {
        anim = GetComponent<Animator>();
        target = GameObject.Find("Player");

        vida = GetComponent<EnemyHealth>();
        if (vida != null) vida.OnMuerte += Morir;
    }

    private void OnDestroy()
    {
        if (vida != null) vida.OnMuerte -= Morir;
    }

    public void Update()
    {
        if (muerto) return;
        Comportamientos();
    }

    private void Morir()
    {
        muerto = true;
        atacando = false;
        anim.SetBool("walk", false);
        anim.SetBool("run", false);
        anim.SetBool("attack", false);
        anim.SetBool("dead", true);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rango != null) rango.GetComponent<BoxCollider2D>().enabled = false;

        Destroy(gameObject, 3f);
    }

    public void Comportamientos()
    {

        if (Mathf.Abs(transform.position.x - target.transform.position.x) > rango_vision && !atacando)
        {
            anim.SetBool("run", false);
            cronometro += 1 * Time.deltaTime;
            if (cronometro >= 4)
            {
                rutina = Random.Range(0, 2);
                cronometro = 0;
            }

            switch (rutina)
            {
                case 0:
                    anim.SetBool("walk", false);
                    break;
                case 1:
                    direccion = Random.Range(0, 2);
                    rutina++;
                    break;
                case 2:
                    switch (direccion)
                    {
                        case 0:
                            transform.rotation = Quaternion.Euler(0, 0, 0);
                            transform.Translate(Vector3.right * speed_walk * Time.deltaTime);
                            break;
                        case 1:
                            transform.rotation = Quaternion.Euler(0, 180, 0);
                            transform.Translate(Vector3.right * speed_walk * Time.deltaTime);
                            break;

                    }
                    anim.SetBool("walk", true);
                    break;

            }
        }
        else if (Mathf.Abs(transform.position.x - target.transform.position.x) > rango_ataque && !atacando)
        {
            // Dentro del rango de visión pero todavía lejos para golpear: corre hacia el jugador.
            if (transform.position.x < target.transform.position.x)
            {
                anim.SetBool("walk", false);
                anim.SetBool("run", true);
                transform.Translate(Vector3.right * speed_run * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                anim.SetBool("attack", false);

            }
            else
            {
                anim.SetBool("walk", false);
                anim.SetBool("run", true);
                transform.Translate(Vector3.right * speed_run * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, 180, 0);
                anim.SetBool("attack", false);
            }
        }
        else
        {
            // Ya está a distancia de golpe: se detiene, mira al jugador y ataca.
            if (transform.position.x < target.transform.position.x)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            anim.SetBool("walk", false);
            anim.SetBool("run", false);

            if (!atacando && Time.time >= tiempoUltimoAtaque + tiempoEntreAtaques)
            {
                atacando = true;
                tiempoUltimoAtaque = Time.time;
                anim.SetBool("attack", true);
            }
        }
    }

    public void Final_Anim()
    {
        anim.SetBool("attack", false);
        atacando = false;
        rango.GetComponent<BoxCollider2D>().enabled = false;
    }
    public void ColliderWeaponTrue()
    {
        Hit.GetComponent<BoxCollider2D>().enabled = true;
    }
    public void ColliderWeaponFalse()
    {
        Hit.GetComponent<BoxCollider2D>().enabled = false;
    }
}

