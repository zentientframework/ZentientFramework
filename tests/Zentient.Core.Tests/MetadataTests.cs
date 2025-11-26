using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using FluentAssertions;

using Zentient.Metadata;

namespace Zentient.Metadata.Tests
{
    public sealed class MetadataTests
    {
        [Fact]
        public void Empty_HasZeroEntries()
        {
            var m = Metadata.Empty;
            m.Should().BeEmpty();
            m.ContainsKey("x").Should().BeFalse();
        }

        [Fact]
        public void Empty_Set_CreatesLinear()
        {
            var m = Metadata.Empty.Set("a", 1);
            m.Should().HaveCount(1);
            m.ContainsKey("a").Should().BeTrue();
        }

        [Fact]
        public void Empty_Remove_IsNoOp()
        {
            var m = Metadata.Empty.Remove("anything");
            m.Should().BeSameAs(Metadata.Empty);
        }

        // ------------------------------------------------------------
        // BUILDER
        // ------------------------------------------------------------

        [Fact]
        public void Builder_From_CopiesSnapshot()
        {
            var a = Metadata.NewBuilder().Set("a", 1).Build();
            var b = Metadata.Builder.From(a).Build();
            a.Should().NotBeSameAs(b);
            b.Should().HaveCount(1);
        }

        [Fact]
        public void Builder_Sets_MultipleValues()
        {
            var b = Metadata.NewBuilder();
            b.Set("a", 1).Set("b", 2);
            var m = b.Build();

            m.Should().HaveCount(2);
            m.GetOrDefault<int>("a").Should().Be(1);
            m.GetOrDefault<int>("b").Should().Be(2);
        }

        [Fact]
        public void Builder_Produces_Linear_WhenUnderThreshold()
        {
            var b = Metadata.NewBuilder();
            for (int i = 0; i < 4; i++) b.Set("k" + i, i);
            var m = b.Build();

            m.GetType().Name.Should().Contain("Linear");
        }

        [Fact]
        public void Builder_Produces_Hashed_WhenAboveThreshold()
        {
            var b = Metadata.NewBuilder();
            for (int i = 0; i < 12; i++) b.Set("k" + i, i);
            var m = b.Build();

            m.GetType().Name.Should().Contain("Hashed");
        }

        [Fact]
        public void Builder_Linear_IsDeterministicallyOrdered()
        {
            var b = Metadata.NewBuilder();
            b.Set("z", 1).Set("a", 2).Set("m", 3);
            var m = b.Build();

            var keys = m.Keys.ToArray();
            keys.Should().Equal(new[] { "a", "m", "z" });
        }

        // ------------------------------------------------------------
        // LINEAR METADATA (direct Set/Remove)
        // ------------------------------------------------------------

        [Fact]
        public void Linear_Set_UpdatesExistingWithoutBuilder()
        {
            var m0 = Metadata.NewBuilder().Set("a", 1).Build();
            m0.GetType().Name.Should().Contain("Linear");

            var m1 = m0.Set("a", 99);

            m1.GetType().Name.Should().Contain("Linear");
            m1.GetOrDefault<int>("a").Should().Be(99);
        }

        [Fact]
        public void Linear_Set_AddsAndSorts()
        {
            var m0 = Metadata.NewBuilder()
                .Set("b", 1)
                .Set("d", 2)
                .Build();

            var m1 = m0.Set("a", 100);
            var keys = m1.Keys.ToArray();

            keys.Should().Equal(new[] { "a", "b", "d" });
        }

        [Fact]
        public void Linear_Remove_RemovesEntry()
        {
            var m0 = Metadata.NewBuilder().Set("a", 1).Set("b", 2).Build();
            var m1 = m0.Remove("a");

            m1.Should().NotContainKey("a");
            m1.Should().ContainKey("b");
        }

        [Fact]
        public void Linear_Remove_LastEntryReturnsEmpty()
        {
            var m0 = Metadata.NewBuilder().Set("a", 1).Build();
            var m1 = m0.Remove("a");

            m1.Should().BeSameAs(Metadata.Empty);
        }

        [Fact]
        public void Linear_Set_PromotesToHashed_WhenThresholdExceeded()
        {
            var b = Metadata.NewBuilder();
            for (int i = 0; i < 8; i++) b.Set("k" + i, i);
            var m = b.Build();
            m.GetType().Name.Should().Contain("Linear");

            // Trigger promotion to hashed
            var promoted = m.Set("k8", 8);
            promoted.GetType().Name.Should().Contain("Hashed");
        }

        // ------------------------------------------------------------
        // HASHED METADATA
        // ------------------------------------------------------------

        [Fact]
        public void Hashed_Set_UpdatesCorrectly()
        {
            var b = Metadata.NewBuilder();
            for (int i = 0; i < 10; i++) b.Set("k" + i, i);
            var m = b.Build();

            var m2 = m.Set("k5", 500);
            m2.GetOrDefault<int>("k5").Should().Be(500);
        }

        [Fact]
        public void Hashed_Remove_DownsizesToLinear()
        {
            var b = Metadata.NewBuilder();
            for (int i = 0; i <= 7; i++) b.Set("k" + i, i); // 8 keys
            var m = b.Build().Set("k8", 8); // Now hashed (9 -> hashed)
            m.GetType().Name.Should().Contain("Hashed");

            var shrunk = m.Remove("k8"); // shrunk.Count == 8 -> should be Linear
            shrunk.GetType().Name.Should().Contain("Linear");
        }

        [Fact]
        public void Hashed_Remove_LastReturnsEmpty()
        {
            var m = Metadata.NewBuilder().Set("x", 1).Build().Set("y", 2).Set("z", 3);
            m = m.Set("a", 4).Remove("x").Remove("y").Remove("z").Remove("a");
            m.Should().BeSameAs(Metadata.Empty);
        }

        // ------------------------------------------------------------
        // DEEP MERGE
        // ------------------------------------------------------------

        [Fact]
        public void DeepMerge_Primitives_LastWriteWins()
        {
            var a = Metadata.NewBuilder().Set("x", 1).Build();
            var b = Metadata.NewBuilder().Set("x", 100).Build();

            var merged = Metadata.DeepMerge(a, b);
            merged.GetOrDefault<int>("x").Should().Be(100);
        }

        [Fact]
        public void DeepMerge_NestedMetadata_Recurses()
        {
            var left = Metadata.NewBuilder()
                .Set("A", Metadata.NewBuilder()
                    .Set("x", 1)
                    .Set("y", 2)
                    .Build())
                .Build();

            var right = Metadata.NewBuilder()
                .Set("A", Metadata.NewBuilder()
                    .Set("y", 20)
                    .Set("z", 30)
                    .Build())
                .Build();

            var merged = Metadata.DeepMerge(left, right);

            var nested = merged.GetOrDefault<IMetadata>("A")!;
            nested.GetOrDefault<int>("x").Should().Be(1);
            nested.GetOrDefault<int>("y").Should().Be(20);
            nested.GetOrDefault<int>("z").Should().Be(30);
        }

        [Fact]
        public void DeepMerge_Resolver_OverridesDefault()
        {
            var a = Metadata.NewBuilder().Set("x", 1).Build();
            var b = Metadata.NewBuilder().Set("x", 100).Build();

            var merged = Metadata.DeepMerge(a, b, (key, oldv, newv) =>
            {
                return oldv; // keep old
            });

            merged.GetOrDefault<int>("x").Should().Be(1);
        }

        // ------------------------------------------------------------
        // TYPED RETRIEVAL + CONVERSION
        // ------------------------------------------------------------

        [Fact]
        public void TryGet_ExactMatch()
        {
            var m = Metadata.NewBuilder().Set("a", 123).Build();
            m.TryGet("a", out int value).Should().BeTrue();
            value.Should().Be(123);
        }

        [Fact]
        public void TryGet_Convert_StringToInt()
        {
            var m = Metadata.NewBuilder().Set("a", "42").Build();
            m.TryGet("a", out int value).Should().BeTrue();
            value.Should().Be(42);
        }

        [Fact]
        public void TryGet_EnumConversion()
        {
            var m = Metadata.NewBuilder().Set("a", "Sunday").Build();
            m.TryGet("a", out DayOfWeek d).Should().BeTrue();
            d.Should().Be(DayOfWeek.Sunday);
        }

        [Fact]
        public void GetOrDefault_UsesDefaultOnMissing()
        {
            var m = Metadata.Empty;
            m.GetOrDefault("missing", 555).Should().Be(555);
        }

        // ------------------------------------------------------------
        // DX HELPERS
        // ------------------------------------------------------------

        [Fact]
        public void SetIfMissing_DoesNotOverwrite()
        {
            var m = Metadata.NewBuilder().Set("a", 10).Build();
            var m2 = m.SetIfMissing("a", 99);

            m2.GetOrDefault<int>("a").Should().Be(10);
            m2.Should().BeSameAs(m);
        }

        [Fact]
        public void SetIfMissing_SetsValueIfMissing()
        {
            var m = Metadata.Empty;
            var m2 = m.SetIfMissing("x", 7);
            m2.GetOrDefault<int>("x").Should().Be(7);
        }

        [Fact]
        public void ReplaceOnly_ReplacesExisting()
        {
            var m = Metadata.NewBuilder().Set("a", 1).Build();
            var m2 = m.ReplaceOnly("a", 22);

            m2.GetOrDefault<int>("a").Should().Be(22);
        }

        [Fact]
        public void ReplaceOnly_NoOpIfMissing()
        {
            var m = Metadata.Empty;
            var m2 = m.ReplaceOnly("x", 999);

            m2.Should().BeSameAs(m);
        }

        [Fact]
        public void Change_TransformsValue()
        {
            var m = Metadata.NewBuilder().Set("a", 10).Build();
            var m2 = m.Change("a", v => (int)v! + 5);

            m2.GetOrDefault<int>("a").Should().Be(15);
        }

        [Fact]
        public void Change_AppliesTransformerEvenWhenMissing()
        {
            var m = Metadata.Empty;
            var m2 = m.Change("x", v => 42);

            m2.GetOrDefault<int>("x").Should().Be(42);
        }

        // ------------------------------------------------------------
        // STABILITY / IMMUTABILITY
        // ------------------------------------------------------------

        [Fact]
        public void Immutability_IsPreserved()
        {
            var m1 = Metadata.NewBuilder().Set("a", 1).Build();
            var m2 = m1.Set("a", 2);

            m1.GetOrDefault<int>("a").Should().Be(1);
            m2.GetOrDefault<int>("a").Should().Be(2);
        }

        [Fact]
        public void Keys_AreDeterministic_InLinear()
        {
            var a = Metadata.NewBuilder()
                .Set("z", 1)
                .Set("b", 2)
                .Set("a", 3)
                .Build();

            a.Keys.Should().Equal(new[] { "a", "b", "z" });
        }
    }
}
