using System;
using System.Collections.Generic;

using Xunit;

using Zentient.Core;

namespace Zentient.Core.Tests
{
    public class LifecycleTests
    {
        [Fact]
        public void Create_WithNoStates_CurrentIsNullAndStatesEmpty()
        {
            var lc = Lifecycle.Create(Array.Empty<(string, DateTimeOffset?)>());
            Assert.NotNull(lc);
            Assert.Empty(lc.States);
            Assert.Null(lc.Current);
        }

        [Fact]
        public void Create_WithStates_PreservesOrder_AndCurrentIsLast()
        {
            var t1 = DateTimeOffset.UtcNow.AddMinutes(-10);
            var t2 = DateTimeOffset.UtcNow.AddMinutes(-5);
            var states = new[] { ("Started", (DateTimeOffset?)t1), ("Running", (DateTimeOffset?)t2) };
            var lc = Lifecycle.Create(states);
            Assert.Equal(2, lc.States.Count);

            var arr = new List<ILifecycleState>(lc.States);
            Assert.Equal("Started", arr[0].Name);
            Assert.Equal(t1, arr[0].EnteredAt);
            Assert.Equal("Running", arr[1].Name);
            Assert.Equal(t2, arr[1].EnteredAt);

            Assert.Equal(arr[1], lc.Current);
        }

        [Fact]
        public void Create_NullName_Throws()
        {
            var states = new[] { (null as string, (DateTimeOffset?)null) };
            Assert.Throws<ArgumentNullException>(() => Lifecycle.Create(states));
        }

        [Fact]
        public void Create_StateWithNullEnteredAt_AllowsNullTimestamp()
        {
            var states = new[] { ("Initial", (DateTimeOffset?)null) };
            var lc = Lifecycle.Create(states);
            Assert.Single(lc.States);
            Assert.Null(lc.Current!.EnteredAt);
        }

        [Fact]
        public void Create_DuplicateStateNames_PreservesAll()
        {
            var states = new[] { ("A", (DateTimeOffset?)null), ("A", (DateTimeOffset?)null) };
            var lc = Lifecycle.Create(states);
            // If implementation allows duplicates, they should both be present in order
            Assert.Equal(2, lc.States.Count);
            Assert.Equal("A", ((List<ILifecycleState>)lc.States)[0].Name);
            Assert.Equal("A", ((List<ILifecycleState>)lc.States)[1].Name);
        }
    }
}
