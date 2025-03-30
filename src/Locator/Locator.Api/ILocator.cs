using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace Locator.Api
{
    public interface ILocator
    {
        public ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken);
    }

    [Serializable]
    public class StandInfo
    {
        // ReSharper disable UnusedMember.Global UnassignedField.Global NotAccessedField.Global
        [RequiredMember] public string Name { get; set; } = string.Empty;
        [RequiredMember] public string Url { get; set; } = string.Empty;
        [RequiredMember] public string? Created { get; set; }
        [RequiredMember] public string? Sha { get; set; }
        // ReSharper restore UnusedMember.Global UnassignedField.Global NotAccessedField.Global
    }
}