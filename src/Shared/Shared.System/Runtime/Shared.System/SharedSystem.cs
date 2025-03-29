using System.Diagnostics;

namespace Shared.System
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
                Debug.Fail("Shared.System.Unity auto reference failed");
#else
                var assembly = global::System.Reflection.Assembly.Load("Shared.System.Asp");
                Debug.Assert(assembly != null);
                var type = assembly.GetType("Shared.System.AspSharedSystem");
                Debug.Assert(type != null);
                var instance = global::System.Activator.CreateInstance(type);
                Debug.Assert(instance != null);
                _instance = (ISharedSystem)instance;
#endif
                return _instance;
            }
        }

        internal static void SetInstance(ISharedSystem sharedSystem) => _instance = sharedSystem;
    }
}