// <copyright file="ITraceSink.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Zentient.Metadata;

namespace Zentient.Diagnostics
{
    using System;

    /// <summary>
    /// Tiny pluggable trace sink. Begin returns a disposable scope for timing/tracing operations.
    /// </summary>
    public interface ITraceSink
    {
        /// <summary>
        /// Begin a tracing scope for the specified activity.
        /// </summary>
        IDisposable Begin(string activityName);
    }
}
