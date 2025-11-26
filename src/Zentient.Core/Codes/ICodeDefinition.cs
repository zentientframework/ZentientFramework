// <copyright file="ICodeDefinition.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;

    using Zentient.Definitions;
    using Zentient.Metadata;

    /// <summary>
    /// Marker interface for a code definition. Implementations represent canonical
    /// definitions (e.g. HttpCodeDefinition, GrpcCodeDefinition).
    /// </summary>
    public interface ICodeDefinition : IDefinition { }
}
