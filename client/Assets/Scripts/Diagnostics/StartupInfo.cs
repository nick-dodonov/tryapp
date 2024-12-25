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
            sb.Append("absoluteURL: ").AppendLine(Application.absoluteURL.ToHumanReadableContent());
            sb.Append("dataPath: ").AppendLine(Application.dataPath);
            sb.Append("persistentDataPath: ").AppendLine(Application.persistentDataPath);
            StaticLog.Info(sb.ToString());
        }
    }
}