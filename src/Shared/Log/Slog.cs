namespace Shared.Log
{
    /// <summary>
    /// Static logger (useful for quick usage without additional setup in ASP or in shared with client code)
    /// </summary>
    public static class Slog
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