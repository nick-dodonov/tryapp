using System;
using System.Threading;
using System.Threading.Tasks;

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
        //[RequiredMember] 
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Created { get; set; }
        public string? Sha { get; set; }
        // ReSharper restore UnusedMember.Global UnassignedField.Global NotAccessedField.Global
    }
}