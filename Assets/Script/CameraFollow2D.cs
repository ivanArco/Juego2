using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Vista")]
    public float zoom = 8f;
    public float suavizadoZoom = 5f;

    [Header("Seguimiento")]
    public float suavizadoMovimiento = 5f;
    public float alturaObjetivo = 1.5f;
    public float anticipacionHorizontal = 1.5f;

    private Camera camara;
    private Vector3 velocidadMovimiento;

    private void Awake()
    {
        camara = GetComponent<Camera>();
        camara.orthographic = true;
        camara.orthographicSize = zoom;
    }

    private void LateUpdate()
    {
        if (objetivo == null)
        {
            return;
        }

        camara.orthographic = true;

        camara.orthographicSize = Mathf.Lerp(
            camara.orthographicSize,
            zoom,
            suavizadoZoom * Time.deltaTime
        );

        float direccion = objetivo.localScale.x >= 0f
            ? 1f
            : -1f;

        Vector3 posicionObjetivo = new Vector3(
            objetivo.position.x +
            direccion * anticipacionHorizontal,

            objetivo.position.y +
            alturaObjetivo,

            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionObjetivo,
            ref velocidadMovimiento,
            1f / suavizadoMovimiento
        );
    }
}