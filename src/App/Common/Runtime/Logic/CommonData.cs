namespace Common.Logic
{
    public static class CommonData
    {
        public static void Initialize()
        {
#if UNITY_5_6_OR_NEWER
            // workaround for the wrong static ctr strip in unity build //TODO: possibly via RuntimeInitializeOnLoadMethod
            Common.Data.ServerState.RegisterFormatter();
#endif
        }
    }
}