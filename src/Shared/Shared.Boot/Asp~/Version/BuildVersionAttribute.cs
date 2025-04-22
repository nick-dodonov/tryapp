// Disable namespace to simplify usage within .csproj AssemblyAttribute
// ReSharper disable CheckNamespace
#pragma warning disable CA1050  // Declare types in namespaces

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildVersionAttribute(
    string timestamp,
    string sha,
    string branch
    ) : Attribute
{
    public string Timestamp { get; } = timestamp;
    public string Sha { get; } = sha;
    public string Branch { get; } = branch;
}

#pragma warning restore CA1050
