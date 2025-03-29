namespace Shared.Web
{
    /// <summary>
    /// Helper converting models the same way as JavaScript or ASP 
    /// </summary>
    public static class WebSerializer
    {
        public static readonly IWebSerializer Default = new SystemWebSerializer();
    }
}