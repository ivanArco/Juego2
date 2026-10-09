using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ZZZ_DiagnosticarBloqueo
{
    [MenuItem("Tools/Fire Knight/ZZZ Diagnosticar Bloqueo Frente al Jugador")]
    public static void Diagnosticar()
    {
        var jugador = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(pc => pc.GetComponent<SpriteRenderer>() != null);

        if (jugador == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el jugador en la escena.", "OK");
            return;
        }

        var sr = jugador.GetComponent<SpriteRenderer>();
        var col = jugador.GetComponent<Collider2D>();
        float direccion = sr.flipX ? -1f : 1f;

        Vector2 centroJugador = col != null ? (Vector2)col.bounds.center : (Vector2)jugador.transform.position;
        float alturaCaja = col != null ? col.bounds.size.y * 0.8f : 1f;
        const float distancia = 1.5f;

        Vector2 centroCaja = centroJugador + new Vector2(direccion * (distancia / 2f + 0.1f), 0f);
        Vector2 tamCaja = new Vector2(distancia, alturaCaja);

        var golpes = Physics2D.OverlapBoxAll(centroCaja, tamCaja, 0f);

        var sb = new StringBuilder();
        sb.AppendLine($"Jugador: '{jugador.name}' en {jugador.transform.position}, mirando {(direccion > 0 ? "derecha" : "izquierda")}");
        sb.AppendLine($"Caja de chequeo: centro={centroCaja}, tamaño={tamCaja}");
        sb.AppendLine($"Objetos encontrados (sin contar al jugador): {golpes.Count(h => h.gameObject != jugador.gameObject)}");
        sb.AppendLine();

        foreach (var hit in golpes)
        {
            if (hit.gameObject == jugador.gameObject) continue;

            var srHit = hit.GetComponent<SpriteRenderer>();
            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(hit.gameObject);

            sb.AppendLine($"- '{hit.gameObject.name}' (layer {LayerMask.LayerToName(hit.gameObject.layer)}, tag {hit.gameObject.tag})");
            sb.AppendLine($"    collider: {hit.GetType().Name}, isTrigger={hit.isTrigger}, enabled={hit.enabled}, bounds={hit.bounds}");
            sb.AppendLine($"    posición: {hit.transform.position}");
            sb.AppendLine($"    SpriteRenderer: {(srHit == null ? "ninguno" : $"enabled={srHit.enabled}, sprite={(srHit.sprite == null ? "NULL" : srHit.sprite.name)}, color={srHit.color}")}");

            if (prefabRoot != null)
            {
                var origenPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                string rutaOrigen = origenPrefab != null ? AssetDatabase.GetAssetPath(origenPrefab) : "(el prefab original ya no existe / referencia rota)";
                sb.AppendLine($"    prefab: raíz='{prefabRoot.name}', origen='{rutaOrigen}'");
            }
            sb.AppendLine();
        }

        string reporte = sb.ToString();
        Debug.Log(reporte);

        string rutaSalida = @"C:\Users\USER\AppData\Local\Temp\claude\E--Unity-Juego2\bcb0b9c9-cae6-4af2-9733-00d31a6991c7\scratchpad\bloqueo_diagnostico.txt";
        System.IO.File.WriteAllText(rutaSalida, reporte);

        EditorUtility.DisplayDialog("Diagnóstico", $"{golpes.Count(h => h.gameObject != jugador.gameObject)} objeto(s) encontrado(s) frente al jugador.\n\nDetalle en la Consola y en:\n{rutaSalida}", "OK");
    }
}
