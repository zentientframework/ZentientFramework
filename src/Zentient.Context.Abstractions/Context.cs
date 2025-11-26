/* PSEUDOCODE / PLAN
 - Goal: Add professional XML documentation comments to public types and members in this file.
 - Steps:
   1. Add a concise developer-facing comment block (this comment) explaining intent.
   2. For each public interface (IContext, IContextDefinition, IContextual<TContext>, IContextBinding<TTarget,TContext>):
      - Add a <summary> describing purpose and intended semantics.
      - For properties add <summary> and explain nullability and typical usage.
      - For generic interfaces add <typeparam> documentation for generic type parameters.
   3. Preserve existing attributes, signatures, and semantics so compilation and behavior remain unchanged.
   4. Keep comments short, precise, and suitable for IntelliSense consumption.
 - Outcome: Improved maintainability and developer experience; no API changes.
*/

namespace Zentient.Context
{
    using Zentient.Core;
    using Zentient.Ontology;

    /// <summary>
    /// Represents a bounded environment or scope in which concepts participate.
    /// Examples include tenant scopes, user sessions, or transactional scopes.
    /// </summary>
    [Ontic(PresenceMode.Actual, BeingKind.Entity)]
    public interface IContext : IDomainConcept
    {
        /// <summary>
        /// Gets the parent context if one exists; otherwise <see langword="null"/>.
        /// Parent contexts may be used to inherit or cascade settings and policies.
        /// </summary>
        IContext? Parent { get; }

        /// <summary>
        /// Gets the scope lattice level that classifies this context (Environment, Tenant, Session, Transaction).
        /// Use this to reason about context visibility and lifecycle boundaries.
        /// </summary>
        ScopeLattice Scope { get; }
    }

    /// <summary>
    /// Represents a definition (meta-model) for a context.
    /// Implementations describe the shape and constraints of runtime <see cref="IContext"/> instances.
    /// </summary>
    public interface IContextDefinition : IContext, IDefinition<IContext> { }

    // Participation

    /// <summary>
    /// Indicates that a concept participates within a specific context.
    /// Use this interface as a mix-in to associate an object with its active context.
    /// </summary>
    /// <typeparam name="TContext">The concrete <see cref="IContext"/> type the object is bound to.</typeparam>
    public interface IContextual<out TContext> : IConcept
        where TContext : IContext
    {
        /// <summary>
        /// Gets the context instance in which the concept is participating.
        /// Implementations SHOULD return a non-null value when the concept is context-bound.
        /// </summary>
        TContext Context { get; }
    }

    // Binding

    /// <summary>
    /// A typed relation that binds a target concept to a context using a binding classification.
    /// This relation describes how and why a target concept is associated with a specific context.
    /// </summary>
    /// <typeparam name="TTarget">The concept being bound to a context.</typeparam>
    /// <typeparam name="TContext">The context type to which the target is bound.</typeparam>
    public interface IContextBinding<out TTarget, out TContext> : IRelation<TTarget, TContext>
        where TTarget : IConcept
        where TContext : IContext
    {
        /// <summary>
        /// Gets the binding type which indicates whether the context is intrinsic to the target
        /// or assigned externally (extrinsic).
        /// </summary>
        ContextBinding BindingType { get; }
    }
}
