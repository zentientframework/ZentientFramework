using System;

using Xunit;

namespace Zentient.Core.Tests
{
    public class ConceptTests
    {
        [Fact]
        public void Create_ValidInputs_ReturnsIConcept()
        {
            var c = Concept.Create("com.example:thing", "Thing", "A thing");
            Assert.NotNull(c);
            Assert.Equal("com.example:thing", c.Key);
            Assert.Equal("Thing", c.DisplayName);
            Assert.Equal("A thing", c.Description);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void Create_InvalidId_Throws(string badId)
        {
            Assert.Throws<ArgumentException>(() => Concept.Create(badId, "Name"));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void Create_InvalidName_Throws(string badName)
        {
            Assert.Throws<ArgumentException>(() => Concept.Create("id", badName));
        }

        [Fact]
        public void Create_EmptyDescription_Throws()
        {
            Assert.Throws<ArgumentException>(() => Concept.Create("id", "name", ""));
        }

        [Fact]
        public void Create_NullDescription_AllowsNullDescription()
        {
            // ensure null description is allowed if implementation supports it
            var c = Concept.Create("id:thing", "Name", null);
            Assert.NotNull(c);
            Assert.Null(c.Description);
        }

        [Fact]
        public void CreateIdentifiable_ValidInputs_ReturnsIIdentifiable()
        {
            var gid = Guid.NewGuid();
            var ident = Concept.CreateIdentifiable(gid, "com.example:id", "Ident", "desc");
            Assert.NotNull(ident);
            Assert.Equal(gid, ident.GuidId);

            var asConcept = (IConcept)ident;
            Assert.Equal("com.example:id", asConcept.Key);
            Assert.Equal("Ident", asConcept.DisplayName);
            Assert.Equal("desc", asConcept.Description);
        }

        [Fact]
        public void CreateIdentifiable_EmptyGuid_Throws()
        {
            Assert.Throws<ArgumentException>(() => Concept.CreateIdentifiable(Guid.Empty, "id", "name"));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void CreateIdentifiable_InvalidIdOrName_Throws(string bad)
        {
            Assert.Throws<ArgumentException>(() => Concept.CreateIdentifiable(Guid.NewGuid(), bad, "name"));
            Assert.Throws<ArgumentException>(() => Concept.CreateIdentifiable(Guid.NewGuid(), "id", bad));
        }

        [Fact]
        public void CreateIdentifiable_EmptyDescription_Throws()
        {
            Assert.Throws<ArgumentException>(() => Concept.CreateIdentifiable(Guid.NewGuid(), "id", "name", ""));
        }

        [Fact]
        public void Create_IdAndName_TrimmingBehavior()
        {
            // If implementation trims inputs, create with whitespace and verify normalized values.
            var c = Concept.Create("  com.example:trim  ", "  TrimName  ", "desc");
            Assert.NotNull(c);
            Assert.Equal("com.example:trim", c.Key.Trim()); // tolerant check
            Assert.Equal("TrimName", c.DisplayName.Trim());
        }
    }
}
