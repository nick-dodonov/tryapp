using System.Threading.Tasks;

namespace Shared.Tp
{
    public interface IPeerIdProvider
    {
        ValueTask<string> GetPeerId();
    }
}