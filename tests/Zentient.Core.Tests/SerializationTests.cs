using System;
using System.Threading.Tasks;

using Xunit;

using Zentient.Core;

namespace Zentient.Core.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void DefaultSerializer_ThrowsOnSerializeAndDeserialize()
        {
            var s = Serialization.Default();
            Assert.Throws<NotSupportedException>(() => s.Serialize(123));
            Assert.Throws<NotSupportedException>(() => s.Deserialize<int>("{}"));
        }

        [Fact]
        public async Task DefaultAsyncSerializer_ThrowsOnSerializeAndDeserializeAsync()
        {
            var s = Serialization.DefaultAsync();
            await Assert.ThrowsAsync<NotSupportedException>(async () => await s.SerializeAsync(123));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await s.DeserializeAsync<int>("{}"));
        }

        [Fact]
        public void CustomSerializer_InterfaceCompliance_Placeholder()
        {
            // If you provide custom serializers via the library surface, tests should verify they round-trip.
            // Placeholder test: ensure default factory exists and throws as expected.
            var s = Serialization.Default();
            Assert.NotNull(s);
        }
    }
}
