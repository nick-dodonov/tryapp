using System.Collections.Generic;
using System.IO;

public static class OptionsReader
{
    public static Dictionary<string, string> ParseEnvFileToDictionary()
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
