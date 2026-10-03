using UnityEngine;

/// <summary>
/// Muestra la vida del jugador usando la animación de 12 frames del asset
/// "Pulsing Heart Anim": el frame 0 es la barra llena y el último frame es la
/// barra vacía. En vez de reproducir la animación, se "congela" en el frame
/// exacto que corresponde al porcentaje de vida actual cada vez que cambia.
/// </summary>
[RequireComponent(typeof(Animator))]
public class MedidorVidaUI : MonoBehaviour
{
    public PlayerHealth vidaJugador;
    [Tooltip("Nombre del estado en el Animator Controller del medidor.")]
    public string nombreEstadoMedidor = "Pulse";

    private Animator animador;

    private void Awake()
    {
        animador = GetComponent<Animator>();
        animador.speed = 0f; // El frame se controla a mano, no se reproduce solo.
        if (vidaJugador == null) vidaJugador = FindObjectOfType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (vidaJugador != null) vidaJugador.OnVidaCambiada += ActualizarMedidor;
    }

    private void OnDisable()
    {
        if (vidaJugador != null) vidaJugador.OnVidaCambiada -= ActualizarMedidor;
    }

    private void Start()
    {
        if (vidaJugador != null) ActualizarMedidor(vidaJugador.VidaActual, vidaJugador.VidaMaxima);
        else ActualizarMedidor(1, 1);
    }

    private void ActualizarMedidor(int actual, int maxima)
    {
        float proporcion = maxima > 0 ? Mathf.Clamp01((float)actual / maxima) : 0f;
        float tiempoNormalizado = 1f - proporcion; // 0 = lleno (frame 0), 1 = vacío (último frame)
        animador.Play(nombreEstadoMedidor, 0, tiempoNormalizado);
        animador.Update(0f); // aplica el frame ya, sin esperar al próximo Update de Unity
    }
}
