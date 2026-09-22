using System;
using UnityEngine;

/// <summary>Vida de un enemigo: recibe daño y avisa cuando muere.</summary>
public class EnemyHealth : MonoBehaviour
{
    public int vidaMaxima = 3;
    [SerializeField] private int vidaActual;

    public bool EstaVivo => vidaActual > 0;
    public int VidaActual => vidaActual;

    public event Action OnDanio;
    public event Action OnMuerte;

    private void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanio(int cantidad)
    {
        if (!EstaVivo || cantidad <= 0) return;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);

        if (vidaActual <= 0)
        {
            OnMuerte?.Invoke();
        }
        else
        {
            OnDanio?.Invoke();
        }
    }
}
