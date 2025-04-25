using System.Diagnostics;

namespace Shared.Sys
{
    public static class SharedSystem
    {
        private static ISharedSystem? _instance;

        internal static ISharedSystem Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

#if UNITY_5_6_OR_NEWER
                Debug.Fail("Shared.Sys.Unity auto reference failed");
#else
                var assembly = System.Reflection.Assembly.Load("Shared.Sys.Asp");
                Debug.Assert(assembly != null);
                var type = assembly.GetType("Shared.Sys.AspSharedSystem");
                Debug.Assert(type != null);
                var instance = System.Activator.CreateInstance(type);
                Debug.Assert(instance != null);
                _instance = (ISharedSystem)instance;
#endif
                return _instance;
            }
        }

        internal static void SetInstance(ISharedSystem sharedSystem) => _instance = sharedSystem;
    }
}