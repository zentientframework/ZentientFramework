using Zentient.Core;

namespace Zentient.DI
{
    public interface ICapability : IConcept
    {
        CapabilityDeclaration Declaration { get; }
    }
    public interface IIntent : IConcept
    {
        CapabilityDeclaration Declaration { get; }
        ActivationPolicy? ActivationPolicy { get; }
    }
    public interface IConceptResolver
    {
        Task<IResolutionResult> ResolveAsync(IIntent intent, CancellationToken token = default);
    }
    // ... ActivationPolicy, Registry etc ...
}