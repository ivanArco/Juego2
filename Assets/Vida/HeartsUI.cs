using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja un corazón por cada punto de vida máxima y los actualiza (lleno/vacío)
/// cada vez que PlayerHealth dispara OnVidaCambiada. Cuando queda 1 corazón,
/// reproduce la animación de pulso importada del .aseprite.
/// </summary>
public class HeartsUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth vidaJugador;
    [Tooltip("Prefab con un componente Image (y opcionalmente un Animator) para cada corazón.")]
    public GameObject prefabCorazon;
    [Tooltip("Objeto con Horizontal Layout Group donde se instancian los corazones.")]
    public Transform contenedor;

    [Header("Sprites estáticos")]
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    [Header("Animación de pulso (opcional)")]
    [Tooltip("Nombre del estado en el Animator Controller del corazón, ej: 'Pulse'.")]
    public string nombreEstadoPulso = "Pulse";
    [Tooltip("Anima el último corazón restante cuando la vida está en 1.")]
    public bool pulsarUltimoCorazon = true;

    private Image[] iconos;
    private Animator[] animadores;
    private int cantidadActual = -1;

    private void Awake()
    {
        if (vidaJugador == null) vidaJugador = FindObjectOfType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (vidaJugador != null)
            vidaJugador.OnVidaCambiada += ActualizarCorazones;
    }

    private void OnDisable()
    {
        if (vidaJugador != null)
            vidaJugador.OnVidaCambiada -= ActualizarCorazones;
    }

    private void Start()
    {
        int max = vidaJugador != null ? vidaJugador.VidaMaxima : 3;
        int actual = vidaJugador != null ? vidaJugador.VidaActual : max;
        ActualizarCorazones(actual, max);
    }

    private void GenerarCorazones(int cantidad)
    {
        if (contenedor == null || prefabCorazon == null) return;

        foreach (Transform hijo in contenedor) Destroy(hijo.gameObject);

        iconos = new Image[cantidad];
        animadores = new Animator[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            GameObject go = Instantiate(prefabCorazon, contenedor);
            iconos[i] = go.GetComponent<Image>();
            animadores[i] = go.GetComponent<Animator>();
        }

        cantidadActual = cantidad;
    }

    private void ActualizarCorazones(int actual, int maxima)
    {
        if (iconos == null || cantidadActual != maxima) GenerarCorazones(maxima);
        if (iconos == null) return;

        for (int i = 0; i < iconos.Length; i++)
        {
            bool lleno = i < actual;
            if (iconos[i] != null)
            {
                if (lleno || corazonVacio != null)
                {
                    iconos[i].sprite = lleno ? corazonLleno : corazonVacio;
                    iconos[i].color = Color.white;
                }
                else
                {
                    // Este asset no trae un sprite de "corazón vacío": se atenúa en vez de quedar en blanco.
                    iconos[i].sprite = corazonLleno;
                    iconos[i].color = new Color(1f, 1f, 1f, 0.25f);
                }
            }

            if (animadores[i] != null)
            {
                bool debePulsar = pulsarUltimoCorazon && lleno && actual == 1 && i == actual - 1;
                animadores[i].enabled = debePulsar;
                if (debePulsar) animadores[i].Play(nombreEstadoPulso, 0, 0f);
            }
        }
    }
}
