using UnityEditor;
using UnityEngine;
using System.IO;

namespace VizDiff.Git
{
    public static class AdapterLocator
    {
        public static string Find()
        {
            // Determine current platform
            string platformFolder;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    platformFolder = "macOS";
                    break;
                case RuntimePlatform.WindowsEditor:
                    platformFolder = "Windows";
                    break;
                case RuntimePlatform.LinuxEditor:
                    platformFolder = "Linux";
                    break;
                default:
                    throw new System.PlatformNotSupportedException($"Unsupported platform: {Application.platform}");
            }

            // Find the adapter binary by name (platform-filtered by Unity)
            var guids = AssetDatabase.FindAssets("git-adapter");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // We only want the one inside our package Runtime/GitAdapter for the current platform
                if (!assetPath.Contains($"/Runtime/GitAdapter/{platformFolder}/"))
                    continue;

                var fullPath = Path.GetFullPath(assetPath);

                if (!File.Exists(fullPath))
                    continue;

                return fullPath;
            }

            throw new FileNotFoundException(
                $"Git adapter binary not found for platform {platformFolder}. " +
                "Make sure the package is installed correctly."
            );
        }
    }
}
