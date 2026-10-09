using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupVida
{
    private const string RutaAseprite = "Assets/Vida/Pulsing Heart Anim Aseprite.aseprite";
    private const string RutaContenedor = "Assets/Vida/ContenedorVida.prefab";
    private const string RutaCorazon = "Assets/Vida/Corazon.prefab";
    private const string RutaEscena = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Fire Knight/Exportar Vista Previa de Vida")]
    public static void ExportarVistaPreviaVida()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(RutaAseprite)
            .OfType<Sprite>()
            .OrderBy(s => ExtraerIndice(s.name))
            .ToList();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No se encontraron sprites en '{RutaAseprite}'.", "OK");
            return;
        }

        const int escala = 6;
        const int separacion = 24;
        int alto = sprites.Max(s => (int)s.rect.height) * escala;
        int ancho = sprites.Sum(s => (int)s.rect.width * escala + separacion);

        var salida = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        var vacio = new Color32[ancho * alto];
        salida.SetPixels32(vacio);

        int cursorX = 0;
        foreach (var sprite in sprites)
        {
            int sx = (int)sprite.rect.x, sy = (int)sprite.rect.y;
            int sw = (int)sprite.rect.width, sh = (int)sprite.rect.height;

            if (!sprite.texture.isReadable)
            {
                EditorUtility.DisplayDialog("Error", $"La textura de '{RutaAseprite}' no es legible (isReadable=false). Reimporta el asset (clic derecho > Reimport) y vuelve a intentar.", "OK");
                Object.DestroyImmediate(salida);
                return;
            }

            Color[] pixeles = sprite.texture.GetPixels(sx, sy, sw, sh);
            for (int py = 0; py < sh; py++)
            {
                for (int px = 0; px < sw; px++)
                {
                    Color c = pixeles[py * sw + px];
                    for (int ey = 0; ey < escala; ey++)
                        for (int ex = 0; ex < escala; ex++)
                            salida.SetPixel(cursorX + px * escala + ex, py * escala + ey, c);
                }
            }
            cursorX += sw * escala + separacion;
        }

        salida.Apply();
        byte[] png = salida.EncodeToPNG();
        Object.DestroyImmediate(salida);

        string rutaSalida = @"C:\Users\USER\AppData\Local\Temp\claude\E--Unity-Juego2\bcb0b9c9-cae6-4af2-9733-00d31a6991c7\scratchpad\vida_preview.png";
        System.IO.File.WriteAllBytes(rutaSalida, png);

        EditorUtility.DisplayDialog("Listo", $"Vista previa exportada ({sprites.Count} frames) en:\n{rutaSalida}", "OK");
    }

    private static int ExtraerIndice(string nombre)
    {
        string digitos = new string(nombre.Where(char.IsDigit).ToArray());
        return int.TryParse(digitos, out int n) ? n : 0;
    }

    private static bool PrepararEscena()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sal del modo Play", "Detén el modo Play antes de ejecutar esta herramienta.", "Entendido");
            return false;
        }

        if (EditorSceneManager.GetActiveScene().path != RutaEscena)
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                bool guardar = EditorUtility.DisplayDialog("Guardar cambios", $"Hay cambios sin guardar en '{EditorSceneManager.GetActiveScene().name}'. ¿Guardarlos antes de abrir SampleScene?", "Guardar", "Descartar");
                if (guardar) EditorSceneManager.SaveOpenScenes();
            }

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        return true;
    }

    [MenuItem("Tools/Fire Knight/Implementar Sistema de Vida")]
    public static void ImplementarVida()
    {
        if (!PrepararEscena()) return;

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(RutaAseprite);
        var sprites = subAssets.OfType<Sprite>().OrderBy(s => ExtraerIndice(s.name)).ToList();
        var controllerMedidor = subAssets.OfType<AnimatorController>().FirstOrDefault();

        if (sprites.Count == 0 || controllerMedidor == null)
        {
            EditorUtility.DisplayDialog("Error", $"No se pudo leer sprites/AnimatorController desde '{RutaAseprite}'.", "OK");
            return;
        }

        // El asset es una animación de 12 frames que va de la barra llena (frame 0) a vacía
        // (último frame) — no son corazones repetidos. Se usa un solo Image+Animator que salta
        // al frame exacto según el % de vida (ver MedidorVidaUI.cs), en vez de instanciar íconos.
        Sprite spriteLleno = sprites[0];
        bool tieneEstado = controllerMedidor.layers.Any(l => l.stateMachine.states.Any(s => s.state.name == "Pulse"));

        // Ya no se usa el prefab de "un corazón" del diseño anterior.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaCorazon) != null)
        {
            AssetDatabase.DeleteAsset(RutaCorazon);
        }

        // 1) Reconstruir ContenedorVida: un solo Image + Animator + MedidorVidaUI.
        var contenidoPrefab = PrefabUtility.LoadPrefabContents(RutaContenedor);

        for (int i = contenidoPrefab.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(contenidoPrefab.transform.GetChild(i).gameObject);
        }

        var layoutViejo = contenidoPrefab.GetComponent<HorizontalLayoutGroup>();
        if (layoutViejo != null) Object.DestroyImmediate(layoutViejo);

        var heartsUIViejo = contenidoPrefab.GetComponent<HeartsUI>();
        if (heartsUIViejo != null) Object.DestroyImmediate(heartsUIViejo);

        var rectPrefab = contenidoPrefab.GetComponent<RectTransform>();
        rectPrefab.sizeDelta = new Vector2(180, 56);

        var imagen = contenidoPrefab.GetComponent<Image>();
        if (imagen == null) imagen = contenidoPrefab.AddComponent<Image>();
        imagen.sprite = spriteLleno;
        imagen.preserveAspect = true;

        var animadorPrefab = contenidoPrefab.GetComponent<Animator>();
        if (animadorPrefab == null) animadorPrefab = contenidoPrefab.AddComponent<Animator>();
        animadorPrefab.runtimeAnimatorController = controllerMedidor;
        animadorPrefab.speed = 0f;

        var medidorPrefab = contenidoPrefab.GetComponent<MedidorVidaUI>();
        if (medidorPrefab == null) medidorPrefab = contenidoPrefab.AddComponent<MedidorVidaUI>();
        medidorPrefab.nombreEstadoMedidor = "Pulse";

        PrefabUtility.SaveAsPrefabAsset(contenidoPrefab, RutaContenedor);
        PrefabUtility.UnloadPrefabContents(contenidoPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2) Canvas en la escena (crea uno si no existe ninguno).
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 3) Reinstanciar el contenedor: borra cualquier instancia vieja para no arrastrar
        //    residuos del diseño anterior (íconos, HorizontalLayoutGroup, etc.).
        var viejas = canvas.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "ContenedorVida" && t != canvas.transform)
            .ToList();
        foreach (var vieja in viejas) Object.DestroyImmediate(vieja.gameObject);

        var prefabContenedorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaContenedor);
        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabContenedorAsset, canvas.transform);
        var rectInstancia = instancia.GetComponent<RectTransform>();
        rectInstancia.anchorMin = new Vector2(0f, 1f);
        rectInstancia.anchorMax = new Vector2(0f, 1f);
        rectInstancia.pivot = new Vector2(0f, 1f);
        rectInstancia.anchoredPosition = new Vector2(30f, -30f);

        // 4) PlayerHealth en el jugador real: el que tiene SpriteRenderer (el Tilemap también tiene
        //    PlayerController por un error aparte, pero no tiene SpriteRenderer, así queda excluido).
        var jugador = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(pc => pc.GetComponent<SpriteRenderer>() != null);

        string avisoJugador;
        if (jugador == null)
        {
            avisoJugador = "\n\nATENCIÓN: no se encontró un jugador válido (PlayerController + SpriteRenderer) para asignarle PlayerHealth.";
        }
        else
        {
            var salud = jugador.GetComponent<PlayerHealth>();
            if (salud == null) salud = jugador.gameObject.AddComponent<PlayerHealth>();

            var medidorEnEscena = instancia.GetComponent<MedidorVidaUI>();
            if (medidorEnEscena != null) medidorEnEscena.vidaJugador = salud;

            EditorUtility.SetDirty(salud);
            avisoJugador = $"\n\nPlayerHealth asignado a '{jugador.name}'.";
        }

        EditorUtility.SetDirty(instancia);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        string avisoEstado = tieneEstado ? "" : "\n\nATENCIÓN: el Animator del medidor no tiene un estado llamado 'Pulse'. Ajusta 'Nombre Estado Medidor' en MedidorVidaUI con el nombre real (ábrelo en el Animator Controller para verlo).";

        EditorUtility.DisplayDialog("Listo", $"Medidor de vida implementado:\n- ContenedorVida ahora es un solo medidor (Image + Animator) que salta al frame según la vida actual, en vez de íconos repetidos\n- Canvas listo con el medidor arriba a la izquierda{avisoJugador}{avisoEstado}\n\nEscena guardada.\n\n(HeartsUI.cs y Corazon.prefab del diseño anterior ya no se usan; se pueden borrar.)", "OK");
    }
}
