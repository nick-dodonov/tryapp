using System.Text;
using Shared;
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
            StaticLog.Info(sb.ToString());
        }
    }
}