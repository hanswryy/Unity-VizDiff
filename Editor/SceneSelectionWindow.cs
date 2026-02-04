using UnityEditor;
using UnityEngine;

public class SceneSelectionWindow : EditorWindow
{
    private SceneAsset scene;

    [MenuItem("Tools/Scene Diff/Compare Scenes")]
    public static void ShowWindow()
    {
        GetWindow<SceneSelectionWindow>("Scene Diff");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Scenes to Compare", EditorStyles.boldLabel);

        scene = (SceneAsset)EditorGUILayout.ObjectField("Scene", scene, typeof(SceneAsset), false);

        EditorGUILayout.Space();

        GUI.enabled = scene != null;
        if (GUILayout.Button("Run Diff"))
        {
            string scenePath = AssetDatabase.GetAssetPath(scene);

            SceneDiffVisualizer.ShowWindow(scenePath);
        }

        GUI.enabled = true;
    }
}
