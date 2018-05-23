namespace NuClear.Broadway.Interfaces.Grains
{
    public interface IVersionedGrain
    {
        int GetCurrentVersion();
    }
}