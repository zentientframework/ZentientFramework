using Zentient.Core;

namespace Zentient.Config
{
    public interface IConfigurationSource : IConcept
    {
        ContextBinding Binding { get; }
        ScopeKind[] Scopes { get; }
    }

    public interface ISetting : IConcept
    {
        string KeyPath { get; }
        object? Value { get; }
        IConfigurationSource Source { get; }
    }
}