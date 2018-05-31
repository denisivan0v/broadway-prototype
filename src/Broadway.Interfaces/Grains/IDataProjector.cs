using System.Threading.Tasks;

namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IDataProjector<in TState>
    {
        Task ProjectAsync(TState state);
    }
}