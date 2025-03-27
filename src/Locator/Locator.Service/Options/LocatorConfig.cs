using System.ComponentModel.DataAnnotations;

namespace Locator.Service.Options;

public class LocatorConfig
{
    [Required]
    public required string StandStackPrefix { get; init; } = "stand-";

    [Required]
    [Url]
    public required string StandUrlTemplate { get; init; }

    public override string ToString() => $"{nameof(LocatorConfig)}(prefix='{StandStackPrefix}' url='{StandUrlTemplate}')";
}