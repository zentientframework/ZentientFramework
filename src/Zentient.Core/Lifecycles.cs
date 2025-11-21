// <copyright file="Lifecycles.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Lifecycle helpers for building simple lifecycle representations.
    /// </summary>
    public static class Lifecycles
    {
        /// <summary>
        /// Creates a new lifecycle instance from the specified collection of state names and entry times.
        /// </summary>
        /// <param name="states">A collection of tuples, each containing the name of a state and the time at which it was entered. The state
        /// name must not be null or empty.</param>
        /// <returns>An <see cref="ILifecycle"/> instance representing the sequence of states and their associated entry times.</returns>
        public static ILifecycle Create(IEnumerable<(string Name, DateTimeOffset? EnteredAt)> states)
        {
            var impl = states.Select(s => (ILifecycleState)new LifecycleStateImpl(s.Name, s.EnteredAt)).ToArray();
            return new LifecycleImpl(impl);
        }

        /// <summary>
        /// Represents the state of a lifecycle at a specific point in time.
        /// </summary>
        internal sealed class LifecycleStateImpl : ILifecycleState
        {
            /// <summary>
            /// Initializes a new instance of the LifecycleStateImpl class with the specified state name and entry time.
            /// </summary>
            /// <param name="name">The name of the lifecycle state. Cannot be null.</param>
            /// <param name="enteredAt">The date and time when the state was entered, or null if unknown.</param>
            /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
            public LifecycleStateImpl(string name, DateTimeOffset? enteredAt)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                EnteredAt = enteredAt;
            }

            /// <inheritdoc/>
            public string Name { get; }

            /// <inheritdoc/>
            public DateTimeOffset? EnteredAt { get; }
        }

        /// <summary>
        /// Represents an immutable sequence of lifecycle states and provides access to the current state.
        /// </summary>
        /// <remarks>This class is intended for internal use and implements the ILifecycle interface. The
        /// collection of states is read-only and reflects the ordered progression of lifecycle states. The current
        /// state is the most recent state in the sequence, or null if no states are present.</remarks>
        internal sealed class LifecycleImpl : ILifecycle
        {
            /// <summary>
            /// Initializes a new instance of the LifecycleImpl class with the specified sequence of lifecycle states.
            /// </summary>
            /// <param name="states">The collection of lifecycle states to initialize the instance with. The last state in the sequence will
            /// be set as the current state. Cannot be null.</param>
            public LifecycleImpl(IEnumerable<ILifecycleState> states)
            {
                var arr = states is ILifecycleState[] sa ? sa : states.ToArray();
                States = Array.AsReadOnly(arr);
                Current = arr.Length == 0 ? default : arr[^1];
            }

            /// <inheritdoc/>
            public IReadOnlyCollection<ILifecycleState> States { get; }

            /// <inheritdoc/>
            public ILifecycleState? Current { get; }
        }
    }
}
