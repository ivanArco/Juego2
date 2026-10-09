using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja la vida del jugador: daño, curación, invulnerabilidad temporal y muerte.
/// Se comunica con la UI (HeartsUI) mediante el evento OnVidaCambiada, así que
/// no necesita ninguna referencia directa a los corazones.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 6;
    [SerializeField] private int vidaActual;

    [Header("Invulnerabilidad tras recibir daño")]
    public float duracionInvulnerabilidad = 1f;
    public float frecuenciaParpadeo = 0.1f;

    [Header("Referencias (opcional, se autocompletan)")]
    public SpriteRenderer spriteRenderer;

    private bool invulnerable;
    private PlayerController controller;
    private Coroutine rutinaInvulnerabilidad;

    public int VidaMaxima => vidaMaxima;
    public int VidaActual => vidaActual;
    public bool EstaVivo => vidaActual > 0;
    public bool EsInvulnerable => invulnerable;

    /// <summary>Se dispara cada vez que la vida cambia. Parámetros: (vidaActual, vidaMaxima).</summary>
    public event Action<int, int> OnVidaCambiada;
    /// <summary>Se dispara cuando el jugador recibe daño (útil para sonido, flash de cámara, etc.).</summary>
    public event Action OnDanio;
    /// <summary>Se dispara cuando la vida llega a 0.</summary>
    public event Action OnMuerte;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        vidaActual = vidaMaxima;
    }

    private void Start()
    {
        // Notifica el estado inicial para que la UI se pinte correcta desde el primer frame.
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    public void RecibirDanio(int cantidad)
    {
        if (invulnerable || !EstaVivo || cantidad <= 0) return;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        OnDanio?.Invoke();
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);

        if (vidaActual <= 0)
        {
            Morir();
        }
        else
        {
            if (rutinaInvulnerabilidad != null) StopCoroutine(rutinaInvulnerabilidad);
            rutinaInvulnerabilidad = StartCoroutine(RutinaInvulnerabilidad());
        }
    }

    public void Curar(int cantidad)
    {
        if (!EstaVivo || cantidad <= 0) return;
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    public void CurarCompleto()
    {
        vidaActual = vidaMaxima;
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    /// <summary>Útil si quieres aumentar la vida máxima a mitad de partida (power-up, etc.).</summary>
    public void AumentarVidaMaxima(int cantidad, bool curarDiferencia = true)
    {
        vidaMaxima += cantidad;
        if (curarDiferencia) vidaActual += cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    private void Morir()
    {
        OnMuerte?.Invoke();
        // Desactiva el control del jugador. Aquí también podrías:
        // - reproducir una animación de muerte,
        // - deshabilitar el collider,
        // - llamar a un GameManager para mostrar pantalla de derrota, etc.
        if (controller != null) controller.enabled = false;
    }

    private IEnumerator RutinaInvulnerabilidad()
    {
        invulnerable = true;
        float t = 0f;
        while (t < duracionInvulnerabilidad)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(frecuenciaParpadeo);
            t += frecuenciaParpadeo;
        }
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        invulnerable = false;
        rutinaInvulnerabilidad = null;
    }

    /// <summary>Llamar desde PlayerController.LlevarARespawn() para restaurar la vida al reaparecer.</summary>
    public void ReiniciarVida()
    {
        if (rutinaInvulnerabilidad != null) StopCoroutine(rutinaInvulnerabilidad);
        invulnerable = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        vidaActual = vidaMaxima;
        if (controller != null) controller.enabled = true;
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }
}
