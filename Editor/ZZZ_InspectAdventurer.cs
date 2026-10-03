using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ZZZ_InspectAdventurer
{
    private const string AsepritePath = "Assets/Player/adventurer_espanol.aseprite";
    private const string ReportPath = "Assets/Player/_ImportReport.txt";

    [MenuItem("Tools/Fire Knight/ZZZ Inspect Adventurer Import")]
    public static void Inspect()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(AsepritePath);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError($"No se pudo cargar nada desde '{AsepritePath}'.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total sub-assets: {assets.Length}");
        sb.AppendLine();

        var byType = assets.GroupBy(a => a.GetType().Name).OrderBy(g => g.Key);
        foreach (var group in byType)
        {
            sb.AppendLine($"=== {group.Key} ({group.Count()}) ===");
            foreach (var a in group.OrderBy(x => x.name))
            {
                sb.AppendLine($"  - {a.name}");
            }
            sb.AppendLine();
        }

        // If there's a GameObject (model prefab root), dump its component/hierarchy structure too.
        var go = assets.OfType<GameObject>().FirstOrDefault();
        if (go != null)
        {
            sb.AppendLine("=== GameObject hierarchy ===");
            DumpHierarchy(go.transform, sb, 0);

            var animator = go.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                sb.AppendLine();
                sb.AppendLine($"=== AnimatorController: {animator.runtimeAnimatorController.name} ===");
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    sb.AppendLine($"  - clip: {clip.name} (length={clip.length:0.00}s, fps={clip.frameRate})");
                }
            }
        }

        File.WriteAllText(ReportPath, sb.ToString());
        AssetDatabase.ImportAsset(ReportPath);
        Debug.Log($"Reporte escrito en {ReportPath}");
    }

    private static void DumpHierarchy(Transform t, System.Text.StringBuilder sb, int depth)
    {
        var comps = t.GetComponents<Component>().Select(c => c.GetType().Name);
        sb.AppendLine($"{new string(' ', depth * 2)}- {t.name} [{string.Join(", ", comps)}]");
        for (int i = 0; i < t.childCount; i++)
            DumpHierarchy(t.GetChild(i), sb, depth + 1);
    }
}
