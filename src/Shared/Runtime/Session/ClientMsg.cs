namespace Shared.Session
{
    /// <summary>
    /// Client initial state stub
    /// TODO: try mempack in webgl
    /// TODO: state diff support
    /// TODO: derived support 
    /// </summary>
    public struct ClientStateMsg
    {
        public int Id;
        public long UtcMs;
        public float X;
        public float Y;
    }
}