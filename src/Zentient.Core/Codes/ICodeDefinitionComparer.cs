// <copyright file="ICodeDefinitionComparer.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using Zentient.Definitions;

    /// <summary>
    /// Defines the contract for comparing two <see cref="ICodeDefinition"/> instances to determine if they are equivalent for caching purposes.
    /// </summary>
    public interface ICodeDefinitionComparer : IDefinitionComparer<ICodeDefinition> { }
}
