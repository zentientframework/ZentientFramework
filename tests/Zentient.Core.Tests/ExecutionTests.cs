using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zentient.Core;

namespace Zentient.Tests
{
    public class ExecutionTests
    {
        [Fact]
        public void Create_ReturnsContext_WithNonCancelledToken()
        {
            using var ctx = Execution.Create();
            Assert.False(ctx.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public void Create_WithCancelledParent_HasCancelledToken()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var ctx = Execution.Create(cts.Token);
            Assert.True(ctx.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public void TrySetAndTryGet_ObjectAndTyped_WorksAndRemove()
        {
            using var ctx = Execution.Create();
            Assert.True(ctx.TrySet("k1", 123, out var reason1));
            Assert.Null(reason1);

            Assert.True(ctx.TryGet("k1", out object? obj, out var reason2));
            Assert.Null(reason2);
            Assert.IsType<int>(obj);
            Assert.Equal(123, (int)obj!);

            Assert.True(ctx.TryGet<int>("k1", out var val, out var reason3));
            Assert.Null(reason3);
            Assert.Equal(123, val);

            // remove by setting null
            Assert.True(ctx.TrySet("k1", (object?)null, out var removeReason));
            Assert.Null(removeReason);
            Assert.False(ctx.TryGet("k1", out _, out var missingReason));
            Assert.NotNull(missingReason);
        }

        [Fact]
        public void TryGet_TypedMismatch_ReturnsFalseWithReason()
        {
            using var ctx = Execution.Create();
            Assert.True(ctx.TrySet("x", 1, out _));
            Assert.False(ctx.TryGet<string>("x", out var str, out var reason));
            Assert.NotNull(reason);
            Assert.Null(str);
        }

        [Fact]
        public void TrySet_DuplicateReturnsFalseWithReason()
        {
            using var ctx = Execution.Create();
            Assert.True(ctx.TrySet("dup", "first", out _));
            Assert.False(ctx.TrySet("dup", "second", out var reason));
            Assert.NotNull(reason);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void TrySet_InvalidKey_Throws(string? badKey)
        {
            using var ctx = Execution.Create();
            Assert.ThrowsAny<ArgumentException>(() => ctx.TrySet(badKey!, "v", out _));
            Assert.ThrowsAny<ArgumentException>(() => ctx.TryGet(badKey!, out _, out _));
        }

        [Fact]
        public void Dispose_CancelsToken_AndFurtherOperationsThrow_ObjectDisposed()
        {
            var ctx = Execution.Create();
            var token = ctx.Cancellation;
            Assert.False(token.IsCancellationRequested);
            ctx.Dispose();
            Assert.True(token.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => ctx.TrySet("x", "y", out _));
        }

        [Fact]
        public void BeginScope_ReturnsDisposable_Noop()
        {
            using var ctx = Execution.Create();
            using var scope = ctx.BeginScope();
            Assert.NotNull(scope);
            // disposing scope shouldn't affect context
            scope.Dispose();
            Assert.False(ctx.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public void ParentCancellation_PropagatesToLinkedContext()
        {
            using var parent = new CancellationTokenSource();
            using var ctx = Execution.Create(parent.Token);
            Assert.False(ctx.Cancellation.IsCancellationRequested);
            parent.Cancel();
            Assert.True(ctx.Cancellation.IsCancellationRequested);
            // after cancellation (but before Dispose) operations should throw OperationCanceledException
            Assert.Throws<OperationCanceledException>(() => ctx.TrySet("a", 1, out _));
        }

        [Fact]
        public void TrySet_RemoveOnMissingKey_ReturnsFalse()
        {
            using var ctx = Execution.Create();
            Assert.False(ctx.TrySet("nope", (object?)null, out var reason));
            Assert.NotNull(reason);
        }

        [Fact]
        public void TrySet_ConcurrentAddAndGet_Works()
        {
            using var ctx = Execution.Create();
            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                ctx.TrySet($"k{i}", i, out _);
                ctx.TryGet<int>($"k{i}", out var v, out _);
                return v;
            })).ToArray();

            Task.WaitAll(tasks);
            Assert.Equal(50, tasks.Length);
            foreach (var t in tasks) Assert.True(t.Result is int);
        }
    }
}
