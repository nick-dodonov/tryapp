using Cysharp.Text;
using Shared.Log;
using UnityEngine;

namespace Shared.Boot.Audit
{
    public static class StartupInfo
    {
        public static void Print()
        {
            string str;
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                // sb.Append("build: ");
                // sb.AppendShortInfo(Shared.Boot.Version.UnityVersionProvider.BuildVersion);
                // sb.AppendLine();

                var absoluteURL = Application.absoluteURL;
                if (!string.IsNullOrEmpty(absoluteURL))
                {
                    sb.Append("absoluteURL: ");
                    sb.AppendLine(absoluteURL);
                }

                sb.Append("dataPath: ");
                sb.AppendLine(Application.dataPath);

                sb.Append("persistentDataPath: ");
                sb.AppendLine(Application.persistentDataPath);

                str = sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }

            Slog.Info(str);
        }
    }
}