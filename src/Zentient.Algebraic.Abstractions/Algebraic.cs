// <copyright file="Algebraic.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Algebraic
{
    using Zentient.Core;

    /// <summary>
    /// Represents a generic algebraic structure within the Zentient ecosystem.
    /// This is a marker interface to group algebraic concepts (categories, monoids, monads, etc.).
    /// </summary>
    public interface IAlgebraicStructure : IConcept { }

    /// <summary>
    /// Marker interface representing a category in the algebraic sense.
    /// Implementations may provide typed objects and morphisms describing the category's structure.
    /// </summary>
    public interface ICategory : IAlgebraicStructure { }

    /// <summary>
    /// Strongly-typed category definition.
    /// </summary>
    /// <typeparam name="TObject">The concept type representing objects in the category.</typeparam>
    /// <typeparam name="TMorphism">The concept type representing morphisms between objects.</typeparam>
    public interface ICategory<out TObject, out TMorphism> : ICategory
        where TObject : IConcept
        where TMorphism : IConcept
    { }

    /// <summary>
    /// Marker interface representing a morphism in an algebraic structure.
    /// A morphism typically has a source and a target object; see <see cref="IMorphism{TSource,TTarget}"/>.
    /// </summary>
    public interface IMorphism : IAlgebraicStructure { }

    /// <summary>
    /// Represents a typed morphism between two concept types.
    /// </summary>
    /// <typeparam name="TSource">The concept type representing the source of the morphism.</typeparam>
    /// <typeparam name="TTarget">The concept type representing the target of the morphism.</typeparam>
    public interface IMorphism<out TSource, out TTarget> : IMorphism
        where TSource : IConcept
        where TTarget : IConcept
    {
        /// <summary>
        /// Gets the source node/object of the morphism.
        /// </summary>
        TSource Source { get; }

        /// <summary>
        /// Gets the target node/object of the morphism.
        /// </summary>
        TTarget Target { get; }
    }

    /// <summary>
    /// Marker interface for functors.
    /// A functor maps objects and morphisms from a source category to a target category.
    /// </summary>
    public interface IFunctor : IAlgebraicStructure { }

    /// <summary>
    /// Represents a typed functor mapping from one category to another.
    /// </summary>
    /// <typeparam name="TSourceCategory">The source category type.</typeparam>
    /// <typeparam name="TTargetCategory">The target category type.</typeparam>
    public interface IFunctor<in TSourceCategory, out TTargetCategory> : IFunctor
        where TSourceCategory : ICategory
        where TTargetCategory : ICategory
    { }

    /// <summary>
    /// Represents a monoid over elements of type <typeparamref name="TElement"/>.
    /// </summary>
    /// <typeparam name="TElement">The element type for the monoid.</typeparam>
    public interface IMonoid<out TElement> : IMonoid where TElement : IConcept { }

    /// <summary>
    /// Marker interface for monoids (algebraic structures with an associative binary operation and an identity element).
    /// </summary>
    public interface IMonoid : IAlgebraicStructure { }

    /// <summary>
    /// Represents a monad parameterized by a container/type <typeparamref name="M"/> and inner type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="M">The monadic container/type.</typeparam>
    /// <typeparam name="T">The contained element type.</typeparam>
    public interface IMonad<out M, out T> : IMonad where M : IConcept where T : IConcept { }

    /// <summary>
    /// Marker interface for monads (structures supporting bind/return semantics).
    /// </summary>
    public interface IMonad : IAlgebraicStructure { }
}
