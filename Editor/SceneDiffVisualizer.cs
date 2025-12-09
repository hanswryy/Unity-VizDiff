using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SceneDiffVisualizer : EditorWindow
{
    private ParsedScene sceneA;
    private ParsedScene sceneB;
    private SceneDiffResult result;

    public static void ShowWindow(string scenePathA, string scenePathB)
    {
        string yamlA = SceneLoader.LoadSceneText(scenePathA);
        string yamlB = SceneLoader.LoadSceneText(scenePathB);

        var parsedA = SceneParser.Parse(yamlA);
        var parsedB = SceneParser.Parse(yamlB);

        var diff = SceneDiffEngine.Compute(parsedA, parsedB);

        var window = GetWindow<SceneDiffVisualizer>("Scene Diff Result");
        window.sceneA = parsedA;
        window.sceneB = parsedB;
        window.result = diff;
    }

    private void OnGUI()
    {
        if (result == null || sceneA == null || sceneB == null)
        {
            GUILayout.Label("No diff result available", EditorStyles.boldLabel);
            return;
        }

        GUILayout.Label("Scene Diff Result", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        DrawSection("Added", result.added, sceneB);
        DrawSection("Removed", result.removed, sceneA);
        DrawSection("Modified", result.modified, sceneB);
    }

    private void DrawSection(string title, List<long> ids, ParsedScene source)
    {
        if (ids == null || source == null || source.objectNames == null)
        {
            GUILayout.Label(title + " (0)", EditorStyles.largeLabel);
            return;
        }

        GUILayout.Label(title + $" ({ids.Count})", EditorStyles.largeLabel);

        foreach (long id in ids)
        {
            if (source.objectNames.ContainsKey(id))
            {
                GUILayout.Label($"• {source.objectNames[id]}  (id {id})");
            }
            else
            {
                GUILayout.Label($"• [Unknown]  (id {id})");
            }
        }

        EditorGUILayout.Space();
    }
}
