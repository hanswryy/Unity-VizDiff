using UnityEditor;
using UnityEngine;

public static class SceneLoader
{
    public static string LoadSceneText(string scenePath)
    {
        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(scenePath);
        if (text == null)
        {
            // Unity stores scenes as binary; load manually from file
            return System.IO.File.ReadAllText(scenePath);
        }

        return text.text;
    }
}
