#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ProjectSnapshot
{
    private static readonly string[] kRoots =
    {
        "Assets/Scenes",
        "Assets/Prefabs",
        "Assets/ScriptableObjects",
        "Assets/Scripts",
        "Assets/ThirdParty" // included if present
    };

    [MenuItem("Tools/Generate Project Snapshot")]
    public static void Generate()
    {
        Directory.CreateDirectory("docs");

        File.WriteAllText("docs/CONTEXT.md", BuildContextMarkdown(), new UTF8Encoding(false));
        File.WriteAllText("docs/assets-files.txt", BuildFileList(), new UTF8Encoding(false));
        File.WriteAllText("docs/assets-dirs.txt", BuildDirList(), new UTF8Encoding(false));

        AssetDatabase.Refresh();
        Debug.Log("Wrote docs/CONTEXT.md, docs/assets-files.txt, docs/assets-dirs.txt");
    }

    private static string BuildContextMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# SliceOfLife-MVP — Context Snapshot");
        sb.AppendLine($"_Generated: {DateTime.Now:yyyy-MM-dd HH:mm}_\n");

        sb.AppendLine("## Scenes");
        var scenes = AssetDatabase.FindAssets("t:Scene", new[] {"Assets/Scenes"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var p in scenes) sb.AppendLine($"- {Path.GetFileNameWithoutExtension(p)}");
        if (!scenes.Any()) sb.AppendLine("- (none)");
        sb.AppendLine();

        sb.AppendLine("## Prefabs");
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets/Prefabs"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var p in prefabs) sb.AppendLine($"- {Rel(p)}");
        if (!prefabs.Any()) sb.AppendLine("- (none)");
        sb.AppendLine();

        sb.AppendLine("## ScriptableObjects");
        var sos = AssetDatabase.FindAssets("t:ScriptableObject", new[] {"Assets/ScriptableObjects"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var p in sos)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(p)?.Name ?? "ScriptableObject";
            sb.AppendLine($"- {Rel(p)}  _(type: {type})_");
        }
        if (!sos.Any()) sb.AppendLine("- (none)");
        sb.AppendLine();

        sb.AppendLine("## Scripts");
        var scripts = AssetDatabase.FindAssets("t:Script", new[] {"Assets/Scripts"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (scripts.Count == 0) sb.AppendLine("- (none)");
        else
        {
            var groups = scripts.GroupBy(p => TopFolderAfter("Assets/Scripts", p));
            foreach (var g in groups.OrderBy(g => g.Key))
            {
                sb.AppendLine($"### {g.Key}");
                foreach (var p in g) sb.AppendLine($"- {Path.GetFileNameWithoutExtension(p)}");
            }
        }
        return sb.ToString();
    }

    private static string BuildFileList()
    {
        var exts = new[] {".cs", ".prefab", ".unity", ".asset"};
        var files = kRoots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(p => exts.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
            .Select(Norm)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        return string.Join("\n", files);
    }

    private static string BuildDirList()
    {
        var lines = kRoots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                .Concat(new[] {root}))
            .Select(Norm)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder("Assets Directory Tree (dirs only)\n\n");
        foreach (var r in kRoots.Where(Directory.Exists)) sb.AppendLine($"[{r}]");
        sb.AppendLine();
        sb.Append(string.Join("\n", lines));
        return sb.ToString();
    }

    private static string Rel(string assetPath) =>
        assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;

    private static string Norm(string path) => path.Replace('\\', '/');

    private static string TopFolderAfter(string root, string assetPath)
    {
        var rel = Norm(assetPath);
        if (!rel.StartsWith(root)) return "(Other)";
        var rest = rel.Substring(root.Length).TrimStart('/');
        var first = rest.Split('/').FirstOrDefault();
        return string.IsNullOrEmpty(first) ? "(Root)" : first;
    }
}
#endif
