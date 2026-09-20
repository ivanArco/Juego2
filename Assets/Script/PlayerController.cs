using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    
    private Rigidbody2D rd2D;

    public float VelMovimiento = 5f;
    float horizontalMovimineto;
    public float suavizadoMovimiento;
    private Vector2 velocidad = Vector2.zero;
    private bool mirandoDerecha = true;

    [Header("Salto")]
    public float fuerzaSalto = 10f;
    public Transform chequeoSuelo;
    public float radioChequeoSuelo = 0.15f;
    public LayerMask capaSuelo;
    private bool enElSuelo;
    private bool saltoSolicitado;


    void Start()
    {
        rd2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float eje = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) eje -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) eje += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame) saltoSolicitado = true;
        }

        horizontalMovimineto = eje * VelMovimiento;
    }

    void FixedUpdate()
    {
        enElSuelo = chequeoSuelo != null && Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);

        Mover(horizontalMovimineto);

        if (saltoSolicitado)
        {
            saltoSolicitado = false;
            if (enElSuelo)
            {
                rd2D.linearVelocity = new Vector2(rd2D.linearVelocity.x, fuerzaSalto);
            }
        }
    }

    public void Mover(float mover)
    {
        Vector2 velocidadObjetivo = new Vector2(mover, rd2D.linearVelocity.y);
        rd2D.linearVelocity = Vector2.SmoothDamp(rd2D.linearVelocity, velocidadObjetivo, ref velocidad, suavizadoMovimiento);

        if (mover > 0 && !mirandoDerecha)
        {
            Girar();
        }
        else if (mover < 0 && mirandoDerecha)
        {
            Girar();
        }
    }


    private void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }
}
