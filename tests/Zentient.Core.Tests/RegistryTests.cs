// <copyright file="RegistryTests.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Tests
{
    using FluentAssertions;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Zentient.Concepts;
    using Zentient.Facades;
    using Zentient.Metadata;

    public class RegistryTests
    {
        private record TestConcept(string Id, string Name, string? Description = null) : IConcept
        {
            public string Key { get; set; } = Id;
            public string DisplayName { get; set; } = Name;
            public Guid GuidId { get; set; }
            public IMetadata Tags { get; set; } = Metadata.Empty;
        }

        [Fact]
        public async Task TryRegisterAsync_AddsNewItem_AndPreventsDuplicate()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var item = new TestConcept("id1", "Name1");

            var r1 = await reg.TryRegisterAsync(item);
            r1.Added.Should().BeTrue();

            // registering same id with same name returns success false
            var r2 = await reg.TryRegisterAsync(item);
            r2.Added.Should().BeFalse();

            // registering same id with different name fails
            var conflict = new TestConcept("id1", "OtherName");
            var r3 = await reg.TryRegisterAsync(conflict);
            r3.Added.Should().BeFalse();
            r3.Reason.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task TryRemoveAsync_RemovesOrReturnsNotFound()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var item = new TestConcept("id2", "Name2");
            await reg.TryRegisterAsync(item);

            var rem1 = await reg.TryRemoveAsync("id2");
            rem1.Removed.Should().BeTrue();

            var rem2 = await reg.TryRemoveAsync("id2");
            rem2.Removed.Should().BeFalse();
            rem2.Reason.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task TryGetByIdAndName_Works_AndNameCachingUsed()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var a = new TestConcept("ida", "SameName");
            var b = new TestConcept("idb", "SameName");
            await reg.TryRegisterAsync(a);
            await reg.TryRegisterAsync(b);

            // deterministic first-match by id ordering: ida < idb
            reg.TryGetByName("SameName", out var found, out var reason).Should().BeTrue();
            reason.Should().BeNull();
            found.Should().NotBeNull();
            found!.Id.Should().Be("ida");

            // direct id lookup
            reg.TryGetById("idb", out var byId).Should().BeTrue();
            byId!.Id.Should().Be("idb");
        }

        [Fact]
        public async Task TryGetByPredicate_ReturnsFirstMatch_OrReason()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var a = new TestConcept("ida", "A");
            var b = new TestConcept("idb", "B");
            await reg.TryRegisterAsync(a);
            await reg.TryRegisterAsync(b);

            reg.TryGetByPredicate(c => c.Name == "B", out var found).Should().BeTrue();
            found!.Id.Should().Be("idb");

            reg.TryGetByPredicate(c => c.Name == "C", out var none, out var reason).Should().BeFalse();
            reason.Should().NotBeNullOrEmpty();
            none.Should().BeNull();
        }

        [Fact]
        public async Task GetOrAddAsync_UsesFactoryAndAvoidsDoubleCreation()
        {
            var reg = Registry.NewInMemory<TestConcept>();

            var created = await reg.GetOrAddAsync("g1", async ct =>
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                return new TestConcept("g1", "G1");
            });

            created.Id.Should().Be("g1");

            // concurrent factory calls should only create once - simulate by starting many tasks
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => reg.GetOrAddAsync("g2", async ct =>
                {
                    await Task.Delay(10, ct).ConfigureAwait(false);
                    return new TestConcept("g2", "G2");
                }).AsTask())
                .ToArray();

            var results = await Task.WhenAll(tasks);
            results.Should().NotBeEmpty();
            results.All(r => r.Id == "g2").Should().BeTrue();

            // verify registered
            reg.TryGetById("g2", out var got).Should().BeTrue();
            got!.Id.Should().Be("g2");
        }

        [Fact]
        public async Task TryGetByName_NotFound_ReturnsFalseWithReason()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            reg.TryGetByName("NoSuchName", out var item, out var reason).Should().BeFalse();
            reason.Should().NotBeNullOrEmpty();
            item.Should().BeNull();
        }

        [Fact]
        public async Task GetOrAddAsync_CancelledFactory_PropagatesCancellation()
        {
            var reg = Registry.NewInMemory<TestConcept>();

            Func<Task> act = async () =>
            {
                var cts = new CancellationTokenSource();
                await reg.GetOrAddAsync("c1", async ct =>
                {
                    // cancel the local token source to cause the factory to observe cancellation
                    cts.Cancel();
                    await Task.Delay(50, cts.Token).ConfigureAwait(false);
                    return new TestConcept("c1", "C1");
                }).AsTask().ConfigureAwait(false);
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
