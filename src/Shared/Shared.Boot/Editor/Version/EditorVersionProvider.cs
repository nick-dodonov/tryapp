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
            var gitSha = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (string.IsNullOrEmpty(gitSha))
                gitSha = TryProcessChecked(GitExecutable, "rev-parse HEAD", "<fail>").Trim();

            //TODO: customize game-ci/unity-builder to obtain more env vars in docker run (for example GITHUB_REF_POINT)
            //  or prepare build version beforehand GITHUB_REF
            const string refPrefix = "refs/heads/";
            var gitRef = Environment.GetEnvironmentVariable("GITHUB_REF");
            if (string.IsNullOrEmpty(gitRef))
                gitRef = TryProcessChecked(GitExecutable, "rev-parse --abbrev-ref HEAD", "<fail>").Trim();
            else if (gitRef.StartsWith(refPrefix))
                gitRef = gitRef[refPrefix.Length..];

            return new()
            {
                Ref = gitRef,
                Sha = gitSha,
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