namespace Client.Utility
{
    public static class ClientApp
    {
        //TODO: add editor feature allowing to simplify debug in unity (possibly ClientOptions editor) 
        public static string AbsoluteUrl => 
            ClientOptions.Instance?.DebugAbsoluteUrl ?? 
            UnityEngine.Application.absoluteURL;
    }
}