#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Baseline "A" exporter for the RQ1 ablation (no scene graph).
///
/// Dumps the active scene as RAW transforms only — the information a generic
/// MCP / editor connection would expose — with NO scene-graph semantics
/// (no SceneNode id / objectType / room). The produced Markdown file is meant
/// to be pasted verbatim into the LLM prompt as the scene context for the
/// "no scene graph" condition, so it can be compared against the hybrid system.
///
/// This is the CONTEXT half of the experiment; EvaluationGroundTruth.cs remains
/// the ANSWER KEY (semantic ground truth) used to grade the LLM's replies.
///
/// Open via:  Tools > Experiment > Raw Scene Dumper (Baseline A)
/// </summary>
public class RawSceneDumper : EditorWindow
{
    public enum ObjectSet
    {
        SceneNodeMatched, // same objects the scene graph tracks (fair, controlled ablation)
        RenderableOnly,   // every GameObject that has a Renderer (physical things only)
        AllObjects        // literally everything (most faithful to raw MCP; noisiest)
    }

    ObjectSet _set = ObjectSet.SceneNodeMatched;
    bool _includeRotationScale = true;
    bool _includeBounds = false;
    bool _includeParentPath = false;
    string _fileName = "RawSceneDump_baselineA.md";

    [MenuItem("Tools/Experiment/Raw Scene Dumper (Baseline A)")]
    public static void Open() => GetWindow<RawSceneDumper>("Raw Scene Dumper");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Baseline A — raw transforms only (no scene graph)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Exports GameObject.name + world transform only. No SceneNode id/type/room.\n" +
            "Paste the resulting file into the LLM prompt for the 'no scene graph' condition.",
            MessageType.Info);

        _set = (ObjectSet)EditorGUILayout.EnumPopup("Object set", _set);
        switch (_set)
        {
            case ObjectSet.SceneNodeMatched:
                EditorGUILayout.HelpBox("Fair ablation: the same objects your scene graph tracks (they carry a SceneNode), but only raw data is written. Recommended for a clean comparison.", MessageType.None);
                break;
            case ObjectSet.RenderableOnly:
                EditorGUILayout.HelpBox("Every object with a Renderer (physical things; excludes managers, cameras, UI text).", MessageType.None);
                break;
            case ObjectSet.AllObjects:
                EditorGUILayout.HelpBox("Literally every GameObject (most faithful to 'just connect MCP'; includes cameras, lights, UI — noisy).", MessageType.None);
                break;
        }

        _includeRotationScale = EditorGUILayout.Toggle("Include rotation + scale", _includeRotationScale);
        _includeBounds = EditorGUILayout.Toggle("Include renderer bounds (AABB)", _includeBounds);
        _includeParentPath = EditorGUILayout.Toggle("Include parent path", _includeParentPath);
        _fileName = EditorGUILayout.TextField("Output file", _fileName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Export dump", GUILayout.Height(30)))
            Export();
    }

    void Export()
    {
        Scene scene = SceneManager.GetActiveScene();

        var all = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
            Collect(root.transform, all);

        var rows = new List<GameObject>();
        foreach (var go in all)
            if (Included(go)) rows.Add(go);

        var sb = new StringBuilder();
        sb.AppendLine("# Scene dump — RAW transforms only (Baseline A: no scene graph)");
        sb.AppendLine();
        sb.AppendLine($"Scene: `{scene.name}`  |  objects: {rows.Count}  |  object set: {_set}");
        sb.AppendLine();
        sb.AppendLine("Coordinate system: Unity, units = metres, left-handed. Positions are world-space.");
        sb.AppendLine("Axes: +X = right (east), -X = left (west); +Y = up, -Y = down; +Z = forward (north), -Z = back (south).");
        sb.AppendLine("Yaw = rotation about Y in degrees (0 = facing +Z/north, 90 = facing +X/east).");
        sb.AppendLine();

        var header = new StringBuilder("| # | Object (GameObject.name) |");
        var sep = new StringBuilder("|---|---|");
        if (_includeParentPath) { header.Append(" Parent path |"); sep.Append("---|"); }
        header.Append(" Position (x, y, z) |"); sep.Append("---|");
        if (_includeRotationScale) { header.Append(" Yaw° | Scale (x, y, z) |"); sep.Append("---|---|"); }
        if (_includeBounds) { header.Append(" Bounds center (x, y, z) | Bounds size (x, y, z) |"); sep.Append("---|---|"); }
        sb.AppendLine(header.ToString());
        sb.AppendLine(sep.ToString());

        int i = 1;
        foreach (var go in rows)
        {
            Transform t = go.transform;
            Vector3 p = t.position;
            var line = new StringBuilder();
            line.Append($"| {i} | {go.name} |");
            if (_includeParentPath) line.Append($" {ParentPath(t)} |");
            line.Append($" ({V(p.x)}, {V(p.y)}, {V(p.z)}) |");
            if (_includeRotationScale)
            {
                Vector3 s = t.lossyScale;
                line.Append($" {V(t.eulerAngles.y)} | ({V(s.x)}, {V(s.y)}, {V(s.z)}) |");
            }
            if (_includeBounds)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    Vector3 c = r.bounds.center, e = r.bounds.size;
                    line.Append($" ({V(c.x)}, {V(c.y)}, {V(c.z)}) | ({V(e.x)}, {V(e.y)}, {V(e.z)}) |");
                }
                else line.Append(" — | — |");
            }
            sb.AppendLine(line.ToString());
            i++;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string path = Path.Combine(projectRoot, _fileName);
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[RawSceneDumper] Wrote {rows.Count} objects ({_set}) -> {path}");
        EditorUtility.RevealInFinder(path);
    }

    static void Collect(Transform t, List<GameObject> acc)
    {
        acc.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            Collect(t.GetChild(i), acc);
    }

    bool Included(GameObject go)
    {
        switch (_set)
        {
            case ObjectSet.RenderableOnly:   return go.GetComponent<Renderer>() != null;
            case ObjectSet.SceneNodeMatched: return HasSceneNode(go);
            default:                         return true;
        }
    }

    // Detected by type name so this Editor script needs no assembly reference to the scene-graph code.
    static bool HasSceneNode(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
            if (c != null && c.GetType().Name == "SceneNode") return true;
        return false;
    }

    static string ParentPath(Transform t)
    {
        var parts = new List<string>();
        for (Transform p = t.parent; p != null; p = p.parent) parts.Insert(0, p.name);
        return parts.Count == 0 ? "(root)" : string.Join("/", parts);
    }

    static string V(float f) => f.ToString("0.##", CultureInfo.InvariantCulture);
}
#endif
