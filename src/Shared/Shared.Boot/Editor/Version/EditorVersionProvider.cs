using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Shared.Boot.Version;

namespace Shared.Boot.Editor.Version
{
    public class EditorVersionProvider : IVersionProvider
    {
        private static string GitExecutable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "git.exe" : "git";

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Initialize()
        {
            if (!EditorVersionOptions.UseRuntimeProvider)
                UnityVersionProvider.SetVersionProvider(new EditorVersionProvider());
        }

        BuildVersion IVersionProvider.ReadBuildVersion()
        {
            var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (string.IsNullOrEmpty(sha))
                sha = TryProcessChecked(GitExecutable, "rev-parse HEAD", "<fail>").Trim();

            var branch = Environment.GetEnvironmentVariable("GITHUB_REF_POINT");
            if (string.IsNullOrEmpty(branch))
                branch = TryProcessChecked(GitExecutable, "rev-parse --abbrev-ref HEAD", "<fail>").Trim();

            return new()
            {
                Sha = sha,
                Branch = branch,
                Time = DateTime.Now 
            };
        }

        private static string ProcessChecked(string exe, string args)
        {
            using var process = Process.Start(new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            Debug.Assert(process != null);

            var sb = new StringBuilder();
            do
            {
                sb.Append(process.StandardOutput.ReadToEnd());
            } while (!process.HasExited);

            if (process.ExitCode != 0)
                throw new($"\"{exe} {args}\", exit code: {process.ExitCode}");

            return sb.ToString();
        }

        private static string TryProcessChecked(string exe, string args, string failResult)
        {
            try
            {
                return ProcessChecked(exe, args);
            }
            catch (Exception)
            {
                return failResult;
            }
        }
    }
}