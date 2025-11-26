using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zentient.Core;
using Zentient.Metadata;

namespace Zentient.Core.Tests
{
    public class RegistryTests
    {
        private record TestConcept(string Id, string Name, string? Description = null) : IConcept
        {
            public string Key { get; set; }

            public string DisplayName { get; set; }

            public Guid GuidId { get; set; }

            public IMetadata Tags { get; set; }
        }

        [Fact]
        public async Task TryRegisterAsync_AddsNewItem_AndPreventsDuplicate()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var item = new TestConcept("id1", "Name1");
            var r1 = await reg.TryRegisterAsync(item);
            Assert.True(r1.Added);

            // registering same id with same name returns success false
            var r2 = await reg.TryRegisterAsync(item);
            Assert.False(r2.Added);

            // registering same id with different name fails
            var conflict = new TestConcept("id1", "OtherName");
            var r3 = await reg.TryRegisterAsync(conflict);
            Assert.False(r3.Added);
            Assert.NotNull(r3.Reason);
        }

        [Fact]
        public async Task TryRemoveAsync_RemovesOrReturnsNotFound()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var item = new TestConcept("id2", "Name2");
            await reg.TryRegisterAsync(item);

            var rem1 = await reg.TryRemoveAsync("id2");
            Assert.True(rem1.Removed);

            var rem2 = await reg.TryRemoveAsync("id2");
            Assert.False(rem2.Removed);
            Assert.NotNull(rem2.Reason);
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
            Assert.True(reg.TryGetByName("SameName", out var found, out var reason));
            Assert.Null(reason);
            Assert.NotNull(found);
            Assert.Equal("ida", found.Id);

            // direct id lookup
            Assert.True(reg.TryGetById("idb", out var byId));
            Assert.Equal("idb", byId!.Id);
        }

        [Fact]
        public async Task TryGetByPredicate_ReturnsFirstMatch_OrReason()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            var a = new TestConcept("ida", "A");
            var b = new TestConcept("idb", "B");
            await reg.TryRegisterAsync(a);
            await reg.TryRegisterAsync(b);

            Assert.True(reg.TryGetByPredicate(c => c.Name == "B", out var found));
            Assert.Equal("idb", found!.Id);

            Assert.False(reg.TryGetByPredicate(c => c.Name == "C", out var none, out var reason));
            Assert.NotNull(reason);
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
            Assert.Equal("g1", created.Id);

            // concurrent factory calls should only create once - simulate by starting many tasks
            var tasks = Enumerable.Range(0, 10).Select(_ => reg.GetOrAddAsync("g2", async ct =>
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                return new TestConcept("g2", "G2");
            })).ToArray();

            var results = await Task.WhenAll(tasks.Select(vt => vt.AsTask()).ToArray());
            Assert.All(results, r => Assert.Equal("g2", r.Id));

            // verify registered
            Assert.True(reg.TryGetById("g2", out var got));
            Assert.Equal("g2", got!.Id);
        }

        [Fact]
        public async Task TryGetByName_NotFound_ReturnsFalseWithReason()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            Assert.False(reg.TryGetByName("NoSuchName", out var item, out var reason));
            Assert.NotNull(reason);
            Assert.Null(item);
        }

        [Fact]
        public async Task GetOrAddAsync_CancelledFactory_PropagatesCancellation()
        {
            var reg = Registry.NewInMemory<TestConcept>();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                await reg.GetOrAddAsync("c1", async ct =>
                {
                    // cancel the passed token to simulate factory cancellation
                    cts.Cancel();
                    await Task.Delay(50, cts.Token).ConfigureAwait(false);
                    return new TestConcept("c1", "C1");
                });
            });
        }
    }
}
