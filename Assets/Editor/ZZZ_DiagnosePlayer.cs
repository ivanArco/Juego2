using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ZZZ_DiagnosePlayer
{
    private const string ReportPath = "Assets/Player/_VisibilityReport.txt";

    [MenuItem("Tools/Fire Knight/ZZZ Diagnose Player Visibility")]
    public static void Diagnose()
    {
        var sb = new System.Text.StringBuilder();

        var controller = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            sb.AppendLine("No se encontró ningún GameObject con el componente PlayerController en la escena activa.");
            File.WriteAllText(ReportPath, sb.ToString());
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log($"Reporte escrito en {ReportPath}");
            return;
        }

        var go = controller.gameObject;
        sb.AppendLine($"GameObject: {go.name}");
        sb.AppendLine($"activeSelf: {go.activeSelf}   activeInHierarchy: {go.activeInHierarchy}");
        sb.AppendLine($"layer: {LayerMask.LayerToName(go.layer)} ({go.layer})");

        var t = go.transform;
        sb.AppendLine($"position: {t.position}   lossyScale: {t.lossyScale}");
        sb.AppendLine();

        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null)
        {
            sb.AppendLine("No hay ningún SpriteRenderer en este GameObject ni en sus hijos.");
        }
        else
        {
            sb.AppendLine($"SpriteRenderer en: {sr.gameObject.name}");
            sb.AppendLine($"  enabled: {sr.enabled}");
            sb.AppendLine($"  sprite: {(sr.sprite == null ? "NULL" : sr.sprite.name)}");
            sb.AppendLine($"  color: {sr.color}");
            sb.AppendLine($"  sortingLayerName: {sr.sortingLayerName}   sortingOrder: {sr.sortingOrder}");
            sb.AppendLine($"  bounds: {sr.bounds}");
            var sg = sr.GetComponentInParent<SortingGroup>();
            sb.AppendLine($"  SortingGroup en padres: {(sg == null ? "ninguno" : $"{sg.name} (sortingOrder={sg.sortingOrder})")}");
        }
        sb.AppendLine();

        var animator = go.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            sb.AppendLine("No hay ningún Animator en este GameObject ni en sus hijos.");
        }
        else
        {
            sb.AppendLine($"Animator en: {animator.gameObject.name}");
            sb.AppendLine($"  enabled: {animator.enabled}");
            sb.AppendLine($"  runtimeAnimatorController: {(animator.runtimeAnimatorController == null ? "NULL" : animator.runtimeAnimatorController.name)}");
            sb.AppendLine($"  cullingMode: {animator.cullingMode}");
        }
        sb.AppendLine();

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            sb.AppendLine($"Rigidbody2D: bodyType={rb.bodyType} gravityScale={rb.gravityScale} constraints={rb.constraints}");
        }
        sb.AppendLine();

        var hit = Physics2D.Raycast(t.position, Vector2.down, 20f);
        if (hit.collider != null)
        {
            sb.AppendLine($"Raycast hacia abajo (20u): golpea '{hit.collider.name}' a distancia {hit.distance:0.00} (tipo {hit.collider.GetType().Name})");
        }
        else
        {
            sb.AppendLine("Raycast hacia abajo (20u): NO golpea nada -> no hay piso debajo, el Rigidbody2D va a caer.");
        }

        var tilemap = Object.FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Include);
        if (tilemap != null)
        {
            var cell = tilemap.WorldToCell(t.position);
            var tileHere = tilemap.GetTile(cell);
            var tileBelow = tilemap.GetTile(cell + new Vector3Int(0, -1, 0));
            sb.AppendLine($"Tilemap '{tilemap.name}': celda del jugador {cell} -> tile ahí: {(tileHere == null ? "vacío" : tileHere.name)}, tile justo abajo: {(tileBelow == null ? "vacío" : tileBelow.name)}");
            sb.AppendLine($"  Tilemap bounds: origin={tilemap.origin} size={tilemap.size} cellSize={tilemap.cellSize}");
        }
        sb.AppendLine();

        var cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine($"Main Camera: position={cam.transform.position}  orthographicSize={cam.orthographicSize}  aspect={cam.aspect}");
            var viewportPoint = cam.WorldToViewportPoint(t.position);
            bool inView = viewportPoint.z > 0 && viewportPoint.x is >= 0 and <= 1 && viewportPoint.y is >= 0 and <= 1;
            sb.AppendLine($"  Posición del jugador en viewport: {viewportPoint}  (dentro de cámara: {inView})");
        }
        else
        {
            sb.AppendLine("No se encontró Camera.main.");
        }

        File.WriteAllText(ReportPath, sb.ToString());
        AssetDatabase.ImportAsset(ReportPath);
        Debug.Log($"Reporte escrito en {ReportPath}");
    }
}
