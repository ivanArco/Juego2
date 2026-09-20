using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ZZZ_TempCleanup
{
    [MenuItem("Tools/Fire Knight/ZZZ Remove Generated Scene Objects")]
    public static void Cleanup()
    {
        int removed = 0;

        foreach (var name in new[] { "Player", "Level Environment", "HUD" })
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
                removed++;
                Debug.Log($"Eliminado de la escena: {name}");
            }
        }

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent("CameraFollow");
            if (follow != null)
            {
                Object.DestroyImmediate(follow);
                removed++;
                Debug.Log("Eliminado el componente CameraFollow de Main Camera.");
            }
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Limpieza completa: {removed} elemento(s) eliminado(s). Guarda la escena (Ctrl+S).");
        }
        else
        {
            Debug.Log("No se encontró nada que limpiar en la escena activa.");
        }
    }
}
