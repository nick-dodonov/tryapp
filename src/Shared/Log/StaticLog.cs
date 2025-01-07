namespace Shared
{
    public static class StaticLog
    {
        public static void Info(string message)
        {
#if UNITY_5_6_OR_NEWER
            UnityEngine.Debug.Log(message);
#else
            //TODO: setup with ILogger
            System.Console.WriteLine(message);
#endif
        }
    }
}