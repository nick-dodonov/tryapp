// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// https://docs.unity3d.com/Manual/CSharpCompiler.html
    ///
    /// C# 9 init and record support comes with a few caveats.
    /// * The type System.Runtime.CompilerServices.IsExternalInit is required for full record support
    ///   as it uses init only setters, but is only available in .NET 5 and later (which Unity doesn’t support).
    ///   Users can work around this issue by declaring the System.Runtime.CompilerServices.IsExternalInit type
    ///   in their own projects.
    /// * You shouldn’t use C# records in serialized types because Unity’s serialization system doesn’t support C# records.
    ///
    /// </summary>
    public static class IsExternalInit
    {
    }
}