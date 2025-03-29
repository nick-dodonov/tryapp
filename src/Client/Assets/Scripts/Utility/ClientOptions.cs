using System;

namespace Client.Utility
{
    [Serializable]
    public class ClientOptions
    {
        public string Locator;
        public string[] Servers;
    }
}