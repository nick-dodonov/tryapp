// Disable namespace to simplify usage within .csproj AssemblyAttribute
// ReSharper disable CheckNamespace
#pragma warning disable CA1050  // Declare types in namespaces

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildVersionAttribute(string timestamp) : Attribute
{
    public string Timestamp { get; } = timestamp;
}

#pragma warning restore CA1050
