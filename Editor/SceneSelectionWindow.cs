using UnityEditor;
using UnityEngine;

public class SceneSelectionWindow : EditorWindow
{
    private SceneAsset sceneA;
    private SceneAsset sceneB;

    [MenuItem("Tools/Scene Diff/Compare Scenes")]
    public static void ShowWindow()
    {
        GetWindow<SceneSelectionWindow>("Scene Diff");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Scenes to Compare", EditorStyles.boldLabel);

        sceneA = (SceneAsset)EditorGUILayout.ObjectField("Scene A", sceneA, typeof(SceneAsset), false);
        sceneB = (SceneAsset)EditorGUILayout.ObjectField("Scene B", sceneB, typeof(SceneAsset), false);

        EditorGUILayout.Space();

        GUI.enabled = sceneA != null && sceneB != null;

        if (GUILayout.Button("Run Diff"))
        {
            string pathA = AssetDatabase.GetAssetPath(sceneA);
            string pathB = AssetDatabase.GetAssetPath(sceneB);

            SceneDiffVisualizer.ShowWindow(pathA, pathB);
        }

        GUI.enabled = true;
    }
}
