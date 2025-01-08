[AttributeUsage(AttributeTargets.Assembly)]
#pragma warning disable CA1050
// ReSharper disable once CheckNamespace
public class BuildInfoAttribute(string timestamp) : Attribute
#pragma warning restore CA1050
{
    public string Timestamp { get; } = timestamp;
}
