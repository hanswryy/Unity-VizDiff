using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SceneDiffVisualizer : EditorWindow
{
    private ParsedScene sceneA;
    private ParsedScene sceneB;
    private SceneDiffResult result;

    // get root directory of the repository
    private static string repoPath = Directory.GetParent(Application.dataPath).FullName;

    public static void ShowWindow(string scenePath)
    {
        // Convert asset path to relative path from repo root
        string relativeScenePath = scenePath;
        
        // If scenePath is an asset path (starts with "Assets/"), use it directly
        // Otherwise, try to make it relative to the repo path
        if (!scenePath.StartsWith("Assets/"))
        {
            string fullScenePath = Path.GetFullPath(scenePath);
            if (fullScenePath.StartsWith(repoPath))
            {
                relativeScenePath = fullScenePath.Substring(repoPath.Length + 1);
            }
            else
            {
                Debug.LogError($"Scene path '{scenePath}' is not within the repository '{repoPath}'");
                return;
            }
        }

        // Load current git HEAD commit for the repository
        string yamlHead = GitAdapterClient.GetFile(
            repoPath,
            "4424ec9720610937a70f9918e33bd076cc01637c",
            relativeScenePath
        );

        string yamlSelected = GitAdapterClient.GetFile(
            repoPath,
            "3fdd3320b62c4c13e71b9d0ae0dbd62aa1543390",
            relativeScenePath
        );

        var parsedA = SceneParser.Parse(yamlHead);
        var parsedB = SceneParser.Parse(yamlSelected);

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
