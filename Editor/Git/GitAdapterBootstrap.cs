using UnityEditor;
using UnityEngine;
using VizDiff.Git;

[InitializeOnLoad]
public static class GitAdapterBootstrap
{
    static GitAdapterBootstrap()
    {
        try
        {
            var adapterPath = AdapterLocator.Find();
            Debug.Log($"Found git adapter at: {adapterPath}");
            GitAdapterClient.Start(adapterPath);
            Debug.Log("Git adapter started successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize git adapter: {ex.Message}");
        }
    }
}