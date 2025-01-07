using System.Text;
using Shared.Log;
using UnityEngine;

namespace Diagnostics
{
    public static class StartupInfo
    {
        public static void Print()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                sb.Append("absoluteURL: ").AppendLine(Application.absoluteURL);
            sb.Append("dataPath: ").AppendLine(Application.dataPath);
            sb.Append("persistentDataPath: ").AppendLine(Application.persistentDataPath);
            Slog.Info(sb.ToString());
        }
    }
}