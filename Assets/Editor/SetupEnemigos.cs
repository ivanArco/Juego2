using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupEnemigos
{
    private const string RutaBase = "Assets/Nivel 1/Caballero";
    private const string RutaEscena = "Assets/Scenes/SampleScene.unity";
    private static readonly string[] Knights = { "Knight_1", "Knight_2", "Knight_3" };
    private static readonly string[] Animaciones = { "Idle", "Walk", "Run", "Attack 1", "Attack 2", "Attack 3", "Run+Attack", "Jump", "Hurt", "Dead", "Defend", "Protect" };
    private static readonly HashSet<string> AnimacionesQueHacenLoop = new HashSet<string> { "Idle", "Walk", "Run", "Defend", "Protect" };
    private const int TamanoFrame = 128;
    private const float FramesPorSegundo = 10f;
    private const float EscalaEnemigo = 3f;

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

    [MenuItem("Tools/Fire Knight/Implementar Enemigos Caballero")]
    public static void ImplementarEnemigos()
    {
        if (!PrepararEscena()) return;

        AsegurarCapaEnemigos();
        AssetDatabase.Refresh();

        int capaEnemigos = LayerMask.NameToLayer("Enemigos");
        if (capaEnemigos >= 0)
        {
            // Si no, el que va adelante le bloquea el paso físicamente a los que vienen detrás.
            Physics2D.IgnoreLayerCollision(capaEnemigos, capaEnemigos, true);
        }

        var prefabsCreados = new List<GameObject>();

        foreach (var knight in Knights)
        {
            var clips = ConfigurarSpritesYAnimaciones(knight);
            if (clips == null || !clips.ContainsKey("Idle"))
            {
                Debug.LogWarning($"No se pudo procesar '{knight}' (falta Idle.png o no se generaron sprites).");
                continue;
            }

            var controller = ConstruirAnimatorController(knight, clips);
            var prefab = CrearPrefabEnemigo(knight, controller, clips["Idle"]);
            prefabsCreados.Add(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (prefabsCreados.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo crear ningún enemigo. Revisa la Consola para más detalle.", "OK");
            return;
        }

        string aviso = ColocarEnemigosEnEscena(prefabsCreados);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Listo", $"{prefabsCreados.Count} caballero(s) implementados: persiguen al jugador, atacan de cerca y tienen vida (mueren al llegar a 0).{aviso}\n\nEl jugador también quedó configurado para hacerles daño al atacar.\n\nEscena guardada.", "OK");
    }

    private static void AsegurarCapaEnemigos()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(asset);
        var layers = so.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == "Enemigos") return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            var prop = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = "Enemigos";
                so.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning("No se encontró un slot de capa libre para 'Enemigos'.");
    }

    private static Dictionary<string, AnimationClip> ConfigurarSpritesYAnimaciones(string knight)
    {
        string carpetaKnight = $"{RutaBase}/{knight}";
        string carpetaAnimaciones = $"{carpetaKnight}/Animaciones";
        if (!AssetDatabase.IsValidFolder(carpetaAnimaciones))
        {
            AssetDatabase.CreateFolder(carpetaKnight, "Animaciones");
        }

        var clips = new Dictionary<string, AnimationClip>();
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        foreach (var anim in Animaciones)
        {
            string rutaPng = $"{carpetaKnight}/{anim}.png";
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaPng);
            if (textura == null)
            {
                Debug.LogWarning($"No se encontró '{rutaPng}', se omite esa animación.");
                continue;
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(rutaPng);
            int frames = Mathf.Max(1, textura.width / TamanoFrame);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;

            var metas = new SpriteMetaData[frames];
            for (int i = 0; i < frames; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = $"{anim}_{i}",
                    rect = new Rect(i * TamanoFrame, 0, TamanoFrame, TamanoFrame),
                    pivot = new Vector2(0.5f, 0f),
                    alignment = (int)SpriteAlignment.BottomCenter
                };
            }
            importer.spritesheet = metas;
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(rutaPng)
                .OfType<Sprite>()
                .OrderBy(s => ExtraerIndice(s.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"'{rutaPng}' no generó sprites tras el recorte.");
                continue;
            }

            var clip = new AnimationClip { frameRate = FramesPorSegundo };
            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / FramesPorSegundo, value = sprites[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            if (AnimacionesQueHacenLoop.Contains(anim))
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            if (anim == "Attack 1")
            {
                int frameImpacto = Mathf.Clamp(sprites.Length / 2, 1, sprites.Length - 1);
                int ultimoFrame = sprites.Length - 1;
                var eventos = new[]
                {
                    new AnimationEvent { time = frameImpacto / FramesPorSegundo, functionName = "ColliderWeaponTrue" },
                    new AnimationEvent { time = (frameImpacto + 1) / FramesPorSegundo, functionName = "ColliderWeaponFalse" },
                    new AnimationEvent { time = ultimoFrame / FramesPorSegundo, functionName = "Final_Anim" },
                };
                AnimationUtility.SetAnimationEvents(clip, eventos);
            }

            string rutaClip = $"{carpetaAnimaciones}/{anim.Replace("+", "Y")}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaClip) != null)
            {
                AssetDatabase.DeleteAsset(rutaClip);
            }
            AssetDatabase.CreateAsset(clip, rutaClip);

            clips[anim] = clip;
        }

        return clips.Count > 0 ? clips : null;
    }

    private static int ExtraerIndice(string nombre)
    {
        int guion = nombre.LastIndexOf('_');
        if (guion < 0) return 0;
        return int.TryParse(nombre.Substring(guion + 1), out int n) ? n : 0;
    }

    private static AnimatorController ConstruirAnimatorController(string knight, Dictionary<string, AnimationClip> clips)
    {
        string rutaController = $"{RutaBase}/{knight}/Animaciones/{knight}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaController) != null)
        {
            AssetDatabase.DeleteAsset(rutaController);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(rutaController);
        var raiz = controller.layers[0].stateMachine;

        var estados = new Dictionary<string, AnimatorState>();
        int i = 0;
        foreach (var kv in clips)
        {
            var posicion = new Vector3((i % 6) * 250, (i / 6) * 80, 0);
            var estado = raiz.AddState(kv.Key, posicion);
            estado.motion = kv.Value;
            estados[kv.Key] = estado;
            i++;
        }

        if (estados.TryGetValue("Idle", out var estadoIdle)) raiz.defaultState = estadoIdle;

        // Parámetros y transiciones para el EnemyAI que controla por SetBool("walk"/"run"/"attack", ...).
        controller.AddParameter("walk", AnimatorControllerParameterType.Bool);
        controller.AddParameter("run", AnimatorControllerParameterType.Bool);
        controller.AddParameter("attack", AnimatorControllerParameterType.Bool);

        if (estadoIdle != null && estados.TryGetValue("Walk", out var estadoWalk) && estados.TryGetValue("Run", out var estadoRun))
        {
            AnimatorStateTransition Transicion(AnimatorState desde, AnimatorState hacia)
            {
                var t = desde.AddTransition(hacia);
                t.hasExitTime = false;
                t.duration = 0.05f;
                return t;
            }

            Transicion(estadoIdle, estadoWalk).AddCondition(AnimatorConditionMode.If, 0, "walk");
            Transicion(estadoWalk, estadoIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "walk");
            Transicion(estadoIdle, estadoRun).AddCondition(AnimatorConditionMode.If, 0, "run");
            Transicion(estadoRun, estadoIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "run");
            Transicion(estadoWalk, estadoRun).AddCondition(AnimatorConditionMode.If, 0, "run");
            Transicion(estadoRun, estadoWalk).AddCondition(AnimatorConditionMode.If, 0, "walk");

            if (estados.TryGetValue("Attack 1", out var estadoAtaque))
            {
                var anyAtaque = raiz.AddAnyStateTransition(estadoAtaque);
                anyAtaque.hasExitTime = false;
                anyAtaque.duration = 0.05f;
                anyAtaque.canTransitionToSelf = false;
                anyAtaque.AddCondition(AnimatorConditionMode.If, 0, "attack");

                Transicion(estadoAtaque, estadoIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "attack");
            }

            controller.AddParameter("dead", AnimatorControllerParameterType.Bool);
            if (estados.TryGetValue("Dead", out var estadoMuerte))
            {
                var anyMuerte = raiz.AddAnyStateTransition(estadoMuerte);
                anyMuerte.hasExitTime = false;
                anyMuerte.duration = 0.05f;
                anyMuerte.canTransitionToSelf = false;
                anyMuerte.AddCondition(AnimatorConditionMode.If, 0, "dead");
                // Sin transición de salida: se queda en el último frame de Dead, como corresponde.
            }
        }

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static GameObject CrearPrefabEnemigo(string knight, AnimatorController controller, AnimationClip clipIdle)
    {
        string rutaPrefab = $"{RutaBase}/{knight}.prefab";

        var go = new GameObject(knight);
        var sr = go.AddComponent<SpriteRenderer>();

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var primerasKeyframes = AnimationUtility.GetObjectReferenceCurve(clipIdle, binding);
        if (primerasKeyframes.Length > 0) sr.sprite = primerasKeyframes[0].value as Sprite;

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.95f);
        col.offset = new Vector2(0f, 0.475f);

        int capaEnemigos = LayerMask.NameToLayer("Enemigos");
        if (capaEnemigos >= 0) go.layer = capaEnemigos;

        go.AddComponent<EnemyHealth>();
        var ai = go.AddComponent<EnemyAI>();
        ai.rango_vision = 15f;
        ai.rango_ataque = 2.2f;
        ai.speed_walk = 2f;
        ai.speed_run = 7f;

        // Zona de golpe: hijo con un collider en modo trigger, desactivado hasta que
        // ColliderWeaponTrue() lo prenda (evento de animación a mitad del swing de "Attack 1").
        var zonaGolpe = new GameObject("ZonaGolpe");
        zonaGolpe.transform.SetParent(go.transform, false);
        zonaGolpe.transform.localPosition = new Vector3(0.35f, 0.45f, 0f);
        var colGolpe = zonaGolpe.AddComponent<BoxCollider2D>();
        colGolpe.isTrigger = true;
        colGolpe.size = new Vector2(0.4f, 0.5f);
        colGolpe.enabled = false;
        zonaGolpe.AddComponent<GolpeEnemigo>();

        ai.rango = zonaGolpe;
        ai.Hit = zonaGolpe;

        go.transform.localScale = Vector3.one * EscalaEnemigo;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) != null)
        {
            AssetDatabase.DeleteAsset(rutaPrefab);
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, rutaPrefab);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static string ColocarEnemigosEnEscena(List<GameObject> prefabs)
    {
        var jugador = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(pc => pc.GetComponent<SpriteRenderer>() != null);

        if (jugador == null)
        {
            return "\n\nATENCIÓN: no se encontró al jugador; los enemigos quedaron creados como prefabs en Assets pero no se colocaron en la escena.";
        }

        // Quita instancias viejas de enemigos para poder re-ejecutar esto sin duplicar.
        foreach (var vieja in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
        {
            Object.DestroyImmediate(vieja.gameObject);
        }

        float[] offsets = { 5f, 9f, 13f };
        for (int i = 0; i < prefabs.Count; i++)
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
            float offset = offsets[i % offsets.Length];
            instancia.transform.position = jugador.transform.position + new Vector3(offset, 0f, 0f);
        }

        // capaEnemigos ahora es privado ([SerializeField] private) en PlayerController,
        // así que se asigna vía SerializedObject en vez de acceso directo al campo.
        var soJugador = new SerializedObject(jugador);
        soJugador.FindProperty("capaEnemigos").intValue = LayerMask.GetMask("Enemigos");
        soJugador.ApplyModifiedProperties();

        return $"\n\nSe colocaron {prefabs.Count} enemigos a la derecha del jugador (revisa que no queden flotando o metidos en una pared, y arrástralos donde quieras).";
    }
}
