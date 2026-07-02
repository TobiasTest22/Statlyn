namespace Statlyn.Core.Abstractions
{
    public interface IRawFootballEntity
    {
        string SourceProvider { get; }

        ProviderType ProviderType { get; }
    }
}
