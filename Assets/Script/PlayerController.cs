using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador 2D estilo Metal Slug para Unity 6.
///
/// Requiere:
/// - Rigidbody2D
/// - Animator
/// - BoxCollider2D
///
/// Controles por defecto:
/// - A / Flecha izquierda: moverse a la izquierda
/// - D / Flecha derecha: moverse a la derecha
/// - Ctrl: correr
/// - S / Flecha abajo: agacharse
/// - Espacio: saltar
/// - Shift izquierdo: dash
/// - J, U, I, L, Q, K: ataques
///
/// Para Unity 2022 o anterior:
/// - linearVelocity -> velocity
/// - linearDamping -> drag
/// - angularDamping -> angularDrag
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    // COMPONENTES
    // ═════════════════════════════════════════════════════════════════════

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // ═════════════════════════════════════════════════════════════════════
    // MOVIMIENTO
    // ═════════════════════════════════════════════════════════════════════

    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 6f;
    [SerializeField] private float velocidadCorrer = 20f;
    [SerializeField] private float aceleracionSuelo = 90f;
    [SerializeField] private float aceleracionAire = 45f;

    [Range(0.1f, 1f)]
    [SerializeField] private float multiplicadorAgachado = 0.45f;

    // ═════════════════════════════════════════════════════════════════════
    // SALTO
    // ═════════════════════════════════════════════════════════════════════

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 13f;

    [Range(0.1f, 1f)]
    [SerializeField] private float fuerzaSaltoCorte = 0.45f;

    [Min(1)]
    [SerializeField] private int cantidadSaltos = 2;

    [Min(0f)]
    [SerializeField] private float tiempoCoyote = 0.12f;

    [Min(0f)]
    [SerializeField] private float tiempoBufferSalto = 0.12f;

    [Min(0f)]
    [SerializeField] private float gravedadNormal = 4.5f;

    [Min(1f)]
    [SerializeField] private float multiplicadorCaida = 2.2f;

    [Min(0f)]
    [SerializeField] private float velocidadMaximaCaida = 25f;

    // ═════════════════════════════════════════════════════════════════════
    // DETECCIÓN DE SUELO
    // ═════════════════════════════════════════════════════════════════════

    [Header("Detección de suelo")]
    [Tooltip("Si está vacío, se usa la base del BoxCollider2D.")]
    [SerializeField] private Transform chequeoSuelo;

    [Min(0.01f)]
    [SerializeField] private float radioChequeoSuelo = 0.18f;

    [SerializeField] private LayerMask capaSuelo;

    [Tooltip("Evita detectar el suelo inmediatamente después de saltar.")]
    [Min(0f)]
    [SerializeField] private float bloqueoSueloDespuesDeSaltar = 0.1f;

    // ═════════════════════════════════════════════════════════════════════
    // DASH
    // ═════════════════════════════════════════════════════════════════════

    [Header("Dash")]
    [Min(0f)]
    [SerializeField] private float fuerzaDash = 18f;

    [Min(0.01f)]
    [SerializeField] private float duracionDash = 0.16f;

    [Min(0f)]
    [SerializeField] private float enfriamientoDash = 0.35f;

    // ═════════════════════════════════════════════════════════════════════
    // AGACHADO
    // ═════════════════════════════════════════════════════════════════════

    [Header("Agachado")]
    [Tooltip("Altura del collider al agacharse. 0.6 = 60% de la altura original.")]
    [Range(0.3f, 1f)]
    [SerializeField] private float proporcionAlturaAgachado = 0.6f;

    [Min(0f)]
    [SerializeField] private float margenComprobacionTecho = 0.05f;

    // ═════════════════════════════════════════════════════════════════════
    // COMBATE
    // ═════════════════════════════════════════════════════════════════════

    [Header("Combate")]
    [Min(0.01f)]
    [SerializeField] private float velocidadAnimacionAtaque = 1.6f;

    [Min(0f)]
    [SerializeField] private float tiempoEntreAtaques = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float frenadoAlAtacar = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float proporcionImpacto = 0.4f;

    [Min(0f)]
    [SerializeField] private float alcanceAtaque = 0.8f;

    [Min(0.01f)]
    [SerializeField] private float radioAtaque = 0.6f;

    [Min(0)]
    [SerializeField] private int danioAtaque = 1;

    [SerializeField] private LayerMask capaEnemigos;

    // ═════════════════════════════════════════════════════════════════════
    // SEGURIDAD Y RESPAWN
    // ═════════════════════════════════════════════════════════════════════

    [Header("Seguridad y respawn")]
    [Min(0f)]
    [SerializeField] private float delayInputAlIniciar = 0.2f;

    [SerializeField] private float limiteCaidaY = -15f;

    [SerializeField] private Vector2 puntoRespawn = new Vector2(0f, 2f);

    // ═════════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ═════════════════════════════════════════════════════════════════════

    private Vector2 colliderSizeNormal;
    private Vector2 colliderOffsetNormal;

    private Vector2 colliderSizeAgachado;
    private Vector2 colliderOffsetAgachado;

    private float inputX;

    private bool corriendo;
    private bool agachado;
    private bool mirandoDerecha = true;

    private bool enElSuelo;
    private int saltosRestantes;

    private float contadorCoyote;
    private float contadorBufferSalto;
    private float bloqueoSueloHasta;

    private bool puedeCortarSalto;

    private bool haciendoDash;
    private bool dashDisponible = true;

    private bool ejecutandoAccion;
    private bool hayAtaqueEnCola;

    private string ataqueEnCola;
    private float duracionEnCola;

    private float tiempoUltimoAtaque = -10f;

    private Coroutine rutinaDash;
    private Coroutine rutinaAtaque;

    private float tiempoInicio;
    private string animacionActual = string.Empty;

    // ═════════════════════════════════════════════════════════════════════
    // NOMBRES DE ANIMACIONES
    // ═════════════════════════════════════════════════════════════════════

    private const string ANIMACION_REPOSO = "reposo";
    private const string ANIMACION_CAMINAR = "caminar";
    private const string ANIMACION_CORRER = "correr";
    private const string ANIMACION_AGACHADO = "agachado";
    private const string ANIMACION_CAMINAR_AGACHADO = "caminar-agachado";
    private const string ANIMACION_SALTAR = "saltar";
    private const string ANIMACION_CAER = "caer";
    private const string ANIMACION_DASH = "voltereta";

    // ═════════════════════════════════════════════════════════════════════
    // CICLO DE VIDA
    // ═════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        GuardarDatosCollider();

        rb.gravityScale = gravedadNormal;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // El movimiento se controla manualmente.
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        saltosRestantes = cantidadSaltos;
    }

    private void OnEnable()
    {
        tiempoInicio = Time.time;

        if (boxCollider != null &&
            colliderSizeNormal == Vector2.zero)
        {
            GuardarDatosCollider();
        }

        ReiniciarEstado();
    }

    private void OnDisable()
    {
        CancelarAcciones();

        if (rb != null)
        {
            rb.gravityScale = gravedadNormal;
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null)
            animator.speed = 1f;
    }

    private void Update()
    {
        LeerControles();

        ActualizarTemporizadores();
        AplicarCorteSalto();
        ActualizarAnimacion();
        ComprobarCaidaAlVacio();
    }

    private void FixedUpdate()
    {
        ComprobarSuelo();
        AplicarMovimiento();
        AplicarGravedad();
        ProcesarSalto();
    }

    // ═════════════════════════════════════════════════════════════════════
    // INPUT
    // ═════════════════════════════════════════════════════════════════════

    private void LeerControles()
    {
        // Durante el retraso inicial no se permite controlar al jugador.
        if (Time.time - tiempoInicio <= delayInputAlIniciar)
        {
            inputX = 0f;
            corriendo = false;
            return;
        }

        Keyboard kb = Keyboard.current;

        // Si no existe teclado, se limpian los valores para evitar
        // que el personaje continúe moviéndose con el último input.
        if (kb == null)
        {
            inputX = 0f;
            corriendo = false;
            return;
        }

        inputX = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            inputX -= 1f;

        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            inputX += 1f;

        bool nuevoMovimiento =
            kb.aKey.wasPressedThisFrame ||
            kb.leftArrowKey.wasPressedThisFrame ||
            kb.dKey.wasPressedThisFrame ||
            kb.rightArrowKey.wasPressedThisFrame;

        if (ejecutandoAccion && nuevoMovimiento)
            CancelarAtaque();

        corriendo =
            kb.leftCtrlKey.isPressed ||
            kb.rightCtrlKey.isPressed;

        bool quiereAgacharse =
            kb.sKey.isPressed ||
            kb.downArrowKey.isPressed;

        // No se permite agacharse en el aire, pero si ya está agachado
        // se conserva la postura hasta que sea seguro levantarse.
        ActualizarAgachado(quiereAgacharse && enElSuelo);

        if (kb.spaceKey.wasPressedThisFrame)
            contadorBufferSalto = tiempoBufferSalto;

        if (kb.spaceKey.wasReleasedThisFrame)
            puedeCortarSalto = false;

        if (kb.leftShiftKey.wasPressedThisFrame)
            IntentarDash();

        if (kb.jKey.wasPressedThisFrame)
            Atacar("ataque1", 0.35f);

        if (kb.uKey.wasPressedThisFrame)
            Atacar("ataque2", 0.40f);

        if (kb.iKey.wasPressedThisFrame)
            Atacar("ataque3", 0.45f);

        if (kb.lKey.wasPressedThisFrame)
            Atacar("patada", 0.50f);

        if (kb.qKey.wasPressedThisFrame)
            Atacar("conjurar", 0.30f);

        if (kb.kKey.wasPressedThisFrame)
            Atacar("ataque-aereo1", 0.30f);
    }

    // ═════════════════════════════════════════════════════════════════════
    // SUELO Y TEMPORIZADORES
    // ═════════════════════════════════════════════════════════════════════


    private void CancelarAtaque()
    {
        if (rutinaAtaque != null)
        {
            StopCoroutine(rutinaAtaque);
            rutinaAtaque = null;
        }

        ejecutandoAccion = false;
        hayAtaqueEnCola = false;

        ataqueEnCola = null;
        duracionEnCola = 0f;

        animator.speed = 1f;
        animacionActual = string.Empty;
    }
    private void ComprobarSuelo()
    {
        bool estabaEnElSuelo = enElSuelo;

        if (Time.time < bloqueoSueloHasta)
        {
            enElSuelo = false;
            return;
        }

        Vector2 centro;

        if (chequeoSuelo != null)
        {
            centro = chequeoSuelo.position;
        }
        else
        {
            Bounds bounds = boxCollider.bounds;

            centro = new Vector2(
                bounds.center.x,
                bounds.min.y);
        }

        Collider2D sueloDetectado = Physics2D.OverlapCircle(
            centro,
            radioChequeoSuelo,
            capaSuelo);

        enElSuelo = sueloDetectado != null;

        if (enElSuelo)
        {
            contadorCoyote = tiempoCoyote;

            if (!estabaEnElSuelo)
                saltosRestantes = cantidadSaltos;
        }
    }

    private void ActualizarTemporizadores()
    {
        if (contadorCoyote > 0f)
            contadorCoyote -= Time.deltaTime;

        if (contadorBufferSalto > 0f)
            contadorBufferSalto -= Time.deltaTime;
    }

    // ═════════════════════════════════════════════════════════════════════
    // MOVIMIENTO
    // ═════════════════════════════════════════════════════════════════════

    private void AplicarMovimiento()
    {
        if (haciendoDash)
            return;

        float objetivoX = 0f;

        float aceleracionActual =
            enElSuelo
                ? aceleracionSuelo
                : aceleracionAire;

        if (ejecutandoAccion)
        {
            // Mientras ataca no puede desplazarse libremente.
            objetivoX = 0f;
            aceleracionActual *= Mathf.Max(
                0.1f,
                1f - frenadoAlAtacar);
        }
        else
        {
            float velocidadMaxima =
                corriendo
                    ? velocidadCorrer
                    : velocidadCaminar;

            if (agachado)
                velocidadMaxima *= multiplicadorAgachado;

            objetivoX = inputX * velocidadMaxima;

            if (inputX > 0.1f && !mirandoDerecha)
                Girar();

            else if (inputX < -0.1f && mirandoDerecha)
                Girar();
        }

        float nuevaVelocidadX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            objetivoX,
            aceleracionActual * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(
            nuevaVelocidadX,
            rb.linearVelocity.y);
    }

    private void AplicarGravedad()
    {
        if (haciendoDash)
            return;

        rb.gravityScale =
            rb.linearVelocity.y < -0.1f
                ? gravedadNormal * multiplicadorCaida
                : gravedadNormal;

        if (rb.linearVelocity.y < -velocidadMaximaCaida)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                -velocidadMaximaCaida);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // SALTO
    // ═════════════════════════════════════════════════════════════════════

    private void ProcesarSalto()
    {
        if (contadorBufferSalto <= 0f)
            return;

        if (haciendoDash || ejecutandoAccion)
            return;

        bool puedeHacerSaltoNormal =
            enElSuelo ||
            contadorCoyote > 0f;

        bool puedeHacerSaltoDoble =
            !puedeHacerSaltoNormal &&
            saltosRestantes > 0;

        if (!puedeHacerSaltoNormal && !puedeHacerSaltoDoble)
            return;

        contadorBufferSalto = 0f;

        if (puedeHacerSaltoNormal)
        {
            saltosRestantes = cantidadSaltos - 1;
            contadorCoyote = 0f;
        }
        else
        {
            saltosRestantes--;
        }

        bloqueoSueloHasta =
            Time.time + bloqueoSueloDespuesDeSaltar;

        enElSuelo = false;
        puedeCortarSalto = true;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            fuerzaSalto);

        ReproducirForzada(ANIMACION_SALTAR);
    }

    private void AplicarCorteSalto()
    {
        Keyboard kb = Keyboard.current;

        if (kb != null &&
            puedeCortarSalto &&
            kb.spaceKey.wasReleasedThisFrame &&
            rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * fuerzaSaltoCorte);

            puedeCortarSalto = false;
        }

        if (enElSuelo || rb.linearVelocity.y <= 0f)
            puedeCortarSalto = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    // AGACHADO
    // ═════════════════════════════════════════════════════════════════════

    private void GuardarDatosCollider()
    {
        colliderSizeNormal = boxCollider.size;
        colliderOffsetNormal = boxCollider.offset;

        float alturaAgachado =
            colliderSizeNormal.y * proporcionAlturaAgachado;

        colliderSizeAgachado = new Vector2(
            colliderSizeNormal.x,
            alturaAgachado);

        // Mantiene los pies en la misma posición.
        colliderOffsetAgachado = new Vector2(
            colliderOffsetNormal.x,
            colliderOffsetNormal.y -
            (colliderSizeNormal.y - alturaAgachado) * 0.5f);
    }

    private void ActualizarAgachado(bool quiereAgacharse)
    {
        // Si el jugador está agachado y hay un techo encima,
        // no se le permite levantarse.
        if (agachado &&
            !quiereAgacharse &&
            HayTechoEncima())
        {
            quiereAgacharse = true;
        }

        if (quiereAgacharse == agachado)
            return;

        agachado = quiereAgacharse;

        boxCollider.size =
            agachado
                ? colliderSizeAgachado
                : colliderSizeNormal;

        boxCollider.offset =
            agachado
                ? colliderOffsetAgachado
                : colliderOffsetNormal;
    }

    private bool HayTechoEncima()
    {
        Vector2 escala = transform.lossyScale;

        Vector2 tamañoNormal = new Vector2(
            Mathf.Abs(colliderSizeNormal.x * escala.x),
            Mathf.Abs(colliderSizeNormal.y * escala.y));

        Vector2 offsetNormal = Vector2.Scale(
            colliderOffsetNormal,
            escala);

        Vector2 centro =
            (Vector2)transform.position +
            offsetNormal;

        // Se comprueba únicamente la zona superior del collider normal.
        Vector2 tamañoComprobacion = new Vector2(
            tamañoNormal.x * 0.9f,
            margenComprobacionTecho);

        Vector2 centroComprobacion = new Vector2(
            centro.x,
            centro.y +
            tamañoNormal.y * 0.5f -
            margenComprobacionTecho * 0.5f);

        Collider2D techo = Physics2D.OverlapBox(
            centroComprobacion,
            tamañoComprobacion,
            0f,
            capaSuelo);

        return techo != null;
    }

    // ═════════════════════════════════════════════════════════════════════
    // DASH
    // ═════════════════════════════════════════════════════════════════════

    private void IntentarDash()
    {
        if (!dashDisponible)
            return;

        if (haciendoDash)
            return;

        if (ejecutandoAccion)
            return;

        rutinaDash = StartCoroutine(RealizarDash());
    }

    private IEnumerator RealizarDash()
    {
        haciendoDash = true;
        dashDisponible = false;

        float direccion =
            mirandoDerecha
                ? 1f
                : -1f;

        rb.gravityScale = 0f;

        rb.linearVelocity = new Vector2(
            direccion * fuerzaDash,
            0f);

        ReproducirForzada(ANIMACION_DASH);

        yield return new WaitForSeconds(duracionDash);

        rb.gravityScale = gravedadNormal;
        haciendoDash = false;
        rutinaDash = null;

        yield return new WaitForSeconds(enfriamientoDash);

        dashDisponible = true;
    }

    // ═════════════════════════════════════════════════════════════════════
    // COMBATE
    // ═════════════════════════════════════════════════════════════════════

    private void Atacar(string nombre, float duracion)
    {
        if (haciendoDash)
            return;

        bool esAtaqueAereo =
            nombre.StartsWith("ataque-aereo");

        if (!enElSuelo && !esAtaqueAereo)
            return;

        // Si ya está atacando, se almacena el siguiente ataque.
        if (ejecutandoAccion)
        {
            hayAtaqueEnCola = true;
            ataqueEnCola = nombre;
            duracionEnCola = duracion;
            return;
        }

        if (Time.time <
            tiempoUltimoAtaque + tiempoEntreAtaques)
        {
            return;
        }

        rutinaAtaque = StartCoroutine(
            RutinaAtaques(nombre, duracion));
    }

    private IEnumerator RutinaAtaques(
        string nombre,
        float duracion)
    {
        while (true)
        {
            ejecutandoAccion = true;
            hayAtaqueEnCola = false;
            tiempoUltimoAtaque = Time.time;

            animator.speed = velocidadAnimacionAtaque;
            ReproducirForzada(nombre);

            float duracionReal =
                duracion / velocidadAnimacionAtaque;

            float tiempoAntesDelImpacto =
                duracionReal * proporcionImpacto;

            float tiempoDespuesDelImpacto =
                duracionReal * (1f - proporcionImpacto);

            yield return new WaitForSeconds(
                tiempoAntesDelImpacto);

            if (ejecutandoAccion)
                AplicarGolpe();

            yield return new WaitForSeconds(
                tiempoDespuesDelImpacto);

            animator.speed = 1f;
            ejecutandoAccion = false;

            if (!hayAtaqueEnCola)
                break;

            yield return new WaitForSeconds(
                tiempoEntreAtaques);

            bool siguienteEsAereo =
                ataqueEnCola.StartsWith("ataque-aereo");

            if (!enElSuelo && !siguienteEsAereo)
                break;

            if (haciendoDash)
                break;

            nombre = ataqueEnCola;
            duracion = duracionEnCola;
        }

        animator.speed = 1f;
        ejecutandoAccion = false;
        hayAtaqueEnCola = false;
        animacionActual = string.Empty;
        rutinaAtaque = null;
    }

    private void AplicarGolpe()
    {
        float direccion =
            mirandoDerecha
                ? 1f
                : -1f;

        Vector2 centro =
            (Vector2)transform.position +
            new Vector2(
                direccion * alcanceAtaque,
                0f);

        Collider2D[] golpeados =
            Physics2D.OverlapCircleAll(
                centro,
                radioAtaque,
                capaEnemigos);

        // Evita dañar varias veces al mismo enemigo
        // si tiene varios colliders.
        HashSet<EnemyHealth> enemigosDaniados =
            new HashSet<EnemyHealth>();

        foreach (Collider2D col in golpeados)
        {
            EnemyHealth salud =
                col.GetComponentInParent<EnemyHealth>();

            if (salud == null)
                continue;

            if (enemigosDaniados.Add(salud))
                salud.RecibirDanio(danioAtaque);
        }
    }

    /// <summary>
    /// Puede llamarse desde un Animation Event si se quiere
    /// sincronizar el daño exactamente con un frame concreto.
    /// </summary>
    public void EventoGolpeAnimacion()
    {
        if (ejecutandoAccion)
            AplicarGolpe();
    }

    // ═════════════════════════════════════════════════════════════════════
    // ANIMACIÓN
    // ═════════════════════════════════════════════════════════════════════

    private void ActualizarAnimacion()
    {
        if (ejecutandoAccion || haciendoDash)
            return;

        if (!enElSuelo)
        {
            Reproducir(
                rb.linearVelocity.y > 0.1f
                    ? ANIMACION_SALTAR
                    : ANIMACION_CAER);

            return;
        }

        if (agachado)
        {
            Reproducir(
                Mathf.Abs(inputX) > 0.1f
                    ? ANIMACION_CAMINAR_AGACHADO
                    : ANIMACION_AGACHADO);

            return;
        }

        if (Mathf.Abs(inputX) < 0.1f)
        {
            Reproducir(ANIMACION_REPOSO);
        }
        else
        {
            Reproducir(
                corriendo
                    ? ANIMACION_CORRER
                    : ANIMACION_CAMINAR);
        }
    }

    private void Reproducir(string nombre)
    {
        if (animator == null)
            return;

        if (animacionActual == nombre)
            return;

        int hash = Animator.StringToHash(
            "Base Layer." + nombre);

        if (!animator.HasState(0, hash))
            return;

        animator.CrossFadeInFixedTime(
            hash,
            0.05f);

        animacionActual = nombre;
    }

    private void ReproducirForzada(string nombre)
    {
        if (animator == null)
            return;

        int hash = Animator.StringToHash(
            "Base Layer." + nombre);

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                $"[PlayerController] No existe el estado " +
                $"'Base Layer.{nombre}' en el Animator.",
                this);

            return;
        }

        animator.Play(hash, 0, 0f);
        animacionActual = nombre;
    }

    private void Girar()
    {
        mirandoDerecha = !mirandoDerecha;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !mirandoDerecha;
    }

    // ═════════════════════════════════════════════════════════════════════
    // RESPAWN Y REINICIO
    // ═════════════════════════════════════════════════════════════════════

    private void CancelarAcciones()
    {
        if (rutinaDash != null)
        {
            StopCoroutine(rutinaDash);
            rutinaDash = null;
        }

        if (rutinaAtaque != null)
        {
            StopCoroutine(rutinaAtaque);
            rutinaAtaque = null;
        }

        haciendoDash = false;
        ejecutandoAccion = false;
        hayAtaqueEnCola = false;

        animator.speed = 1f;
    }

    private void ReiniciarEstado()
    {
        if (rb == null)
            return;

        CancelarAcciones();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = gravedadNormal;

        inputX = 0f;
        corriendo = false;
        enElSuelo = false;

        contadorCoyote = 0f;
        contadorBufferSalto = 0f;
        bloqueoSueloHasta = 0f;

        puedeCortarSalto = false;
        dashDisponible = true;
        saltosRestantes = cantidadSaltos;

        tiempoUltimoAtaque = -10f;
        animacionActual = string.Empty;

        if (agachado)
        {
            agachado = false;
            boxCollider.size = colliderSizeNormal;
            boxCollider.offset = colliderOffsetNormal;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = !mirandoDerecha;
    }

    private void ComprobarCaidaAlVacio()
    {
        if (transform.position.y < limiteCaidaY)
            LlevarARespawn();
    }

    [ContextMenu("Llevar al punto de respawn")]
    public void LlevarARespawn()
    {
        CancelarAcciones();

        // Se mueve el Rigidbody2D, no directamente el Transform.
        rb.position = puntoRespawn;

        ReiniciarEstado();
    }

    // ═════════════════════════════════════════════════════════════════════
    // VALIDACIÓN EN EL INSPECTOR
    // ═════════════════════════════════════════════════════════════════════

    private void OnValidate()
    {
        cantidadSaltos = Mathf.Max(1, cantidadSaltos);
        velocidadAnimacionAtaque =
            Mathf.Max(0.01f, velocidadAnimacionAtaque);

        duracionDash = Mathf.Max(0.01f, duracionDash);
        radioChequeoSuelo = Mathf.Max(0.01f, radioChequeoSuelo);
        radioAtaque = Mathf.Max(0.01f, radioAtaque);

        tiempoCoyote = Mathf.Max(0f, tiempoCoyote);
        tiempoBufferSalto = Mathf.Max(0f, tiempoBufferSalto);
        gravedadNormal = Mathf.Max(0f, gravedadNormal);
        multiplicadorCaida = Mathf.Max(1f, multiplicadorCaida);
    }

    // ═════════════════════════════════════════════════════════════════════
    // GIZMOS
    // ═════════════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D col =
            GetComponent<BoxCollider2D>();

        if (col == null)
            return;

        Vector2 centroSuelo;

        if (chequeoSuelo != null)
        {
            centroSuelo = chequeoSuelo.position;
        }
        else
        {
            Bounds bounds = col.bounds;

            centroSuelo = new Vector2(
                bounds.center.x,
                bounds.min.y);
        }

        Gizmos.color =
            enElSuelo
                ? Color.green
                : Color.red;

        Gizmos.DrawWireSphere(
            centroSuelo,
            radioChequeoSuelo);

        float direccion =
            mirandoDerecha
                ? 1f
                : -1f;

        Vector2 centroAtaque =
            (Vector2)transform.position +
            new Vector2(
                direccion * alcanceAtaque,
                0f);

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            centroAtaque,
            radioAtaque);

        // Zona aproximada de comprobación del techo.
        Vector2 escala = transform.lossyScale;

        Vector2 tamañoNormal = new Vector2(
            Mathf.Abs(colliderSizeNormal.x * escala.x),
            Mathf.Abs(colliderSizeNormal.y * escala.y));

        Vector2 offsetNormal = Vector2.Scale(
            colliderOffsetNormal,
            escala);

        Vector2 centroNormal =
            (Vector2)transform.position +
            offsetNormal;

        Vector2 centroTecho = new Vector2(
            centroNormal.x,
            centroNormal.y +
            tamañoNormal.y * 0.5f -
            margenComprobacionTecho * 0.5f);

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            centroTecho,
            new Vector2(
                tamañoNormal.x * 0.9f,
                margenComprobacionTecho));
    }
}
