using UnityEngine;

/// <summary>
/// Va en el collider de la zona de golpe (el objeto "Hit"/"rango" del EnemyAI).
/// Mientras ColliderWeaponTrue() lo tiene activo, si toca al jugador le hace daño.
/// </summary>
public class GolpeEnemigo : MonoBehaviour
{
    public int danio = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var salud = other.GetComponent<PlayerHealth>();
        if (salud != null) salud.RecibirDanio(danio);
    }
}
