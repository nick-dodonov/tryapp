using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Web;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Utility
{
    public static class ClientOptionsReader
    {
        public static async ValueTask<ClientOptions> ReadOptions()
        {
            try
            {
                var json = await ReadJson();
                if (json != null)
                {
                    var options = WebSerializer.Default.Deserialize<ClientOptions>(json);
                    return options;
                }
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }

            return new();
        }

        private static async ValueTask<string> ReadJson()
        {
            if (Application.isEditor)
            {
                const string optionsJsonPath = "../../pages/options.json";
                if (!File.Exists(optionsJsonPath))
                    return null;
                return await File.ReadAllTextAsync(optionsJsonPath);
            }

            var absoluteUrl = Application.absoluteURL;
            var optionsUri = new Uri(new(absoluteUrl), "options.json");
            var request = UnityWebRequest.Get(optionsUri);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                return null;

            var content = request.downloadHandler.text;
            return content;
        }

        public static Dictionary<string, string> ReadEnvFile()
        {
            var directory = Directory.GetCurrentDirectory();
            Dictionary<string, string> envVariables = new();

            while (!string.IsNullOrEmpty(directory))
            {
                var envFilePath = Path.Combine(directory, ".env");
                if (File.Exists(envFilePath))
                {
                    var lines = File.ReadAllLines(envFilePath);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                            continue;

                        var splitIndex = trimmedLine.IndexOf('=');
                        if (splitIndex <= 0)
                            continue;

                        var key = trimmedLine[..splitIndex].Trim();
                        var value = trimmedLine[(splitIndex + 1)..].Trim();
                        envVariables[key] = value;
                    }

                    break; // Stop searching once the .env file is found and processed
                }

                directory = Path.GetDirectoryName(directory); // Move up one directory
            }

            return envVariables;
        }
    }
}