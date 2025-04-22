// Disable namespace to simplify usage within .csproj AssemblyAttribute
// ReSharper disable CheckNamespace
#pragma warning disable CA1050  // Declare types in namespaces

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildVersionAttribute(
    string @ref,
    string sha,
    string timestamp
    ) : Attribute
{
    public string Ref { get; } = @ref;
    public string Sha { get; } = sha;
    public string Timestamp { get; } = timestamp;
}

#pragma warning restore CA1050
