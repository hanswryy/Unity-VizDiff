using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class GitAdapterClient
{
    static Process process;
    static StreamWriter inputStream;
    static StreamReader outputStream;

    public static void Start(string adapterPath) {
        if (process != null) {
            return;
        }

        if (string.IsNullOrEmpty(adapterPath) || !System.IO.File.Exists(adapterPath))
        {
            throw new System.Exception($"Git adapter binary not found at: {adapterPath}");
        }

        try
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = adapterPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            bool started = process.Start();
            if (!started)
            {
                throw new System.Exception("Failed to start git adapter process");
            }

            inputStream = process.StandardInput;
            outputStream = process.StandardOutput;

            // Give the process a moment to initialize
            System.Threading.Thread.Sleep(100);

            // Check if process is still running
            if (process.HasExited)
            {
                string errorOutput = process.StandardError.ReadToEnd();
                throw new System.Exception($"Git adapter process exited immediately. Error: {errorOutput}");
            }
        }
        catch (System.Exception ex)
        {
            process = null;
            inputStream = null;
            outputStream = null;
            throw new System.Exception($"Failed to start git adapter: {ex.Message}");
        }
    }

    public static string GetFile(string repoPath, string commit, string path) {
        // Ensure process is running and streams are available
        if (process == null || process.HasExited || inputStream == null || outputStream == null)
        {
            throw new System.Exception("Git adapter process is not running. Call Start() first.");
        }

        var json = JsonUtility.ToJson(new GitRequest {
            cmd = "get_file",
            repoPath = repoPath,
            commit = commit,
            path = path
        });

        try
        {
            inputStream.WriteLine(json);
            inputStream.Flush();
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"Failed to write to git adapter process: {ex.Message}");
        }

        var responseJson = outputStream.ReadLine();
        var parsedResponse = JsonUtility.FromJson<GitResponse>(responseJson);

        if (!parsedResponse.ok) {
            throw new System.Exception(parsedResponse.error);
        }

        return parsedResponse.data;
    }

    public static void Shutdown()
    {
        if (process != null && !process.HasExited)
            process.Kill();
    }

    class GitRequest {
        public string cmd;
        public string repoPath;
        public string commit;
        public string path;
    }

    class GitResponse {
        public bool ok;
        public string data;
        public string error;
    }
}