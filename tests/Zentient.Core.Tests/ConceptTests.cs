namespace Zentient.Tests
{
    using FluentAssertions;
    using System;
    using Xunit;
    using Zentient.Concepts;

    // Use FluentAssertions
    public class ConceptTests
    {
        [Fact]
        public void Create_ValidInputs_ReturnsIConcept()
        {
            var c = Concept.Create("com.example:thing", "Thing", "A thing");
            c.Should().BeAssignableTo<IConcept>();
            c.Should().NotBeNull();
            c.Key.Should().Be("com.example:thing");
            c.DisplayName.Should().Be("Thing");
            c.Description.Should().Be("A thing");
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void Create_InvalidId_Throws(string badId)
        {
            Action func = () => Concept.Create(badId, "name");
            func.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void Create_InvalidName_Throws(string badName)
        {
            Action func = () => Concept.Create("id", badName);
            func.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_EmptyDescription_Throws()
        {
            Action func = () => Concept.Create("id:thing", "Name", "");
            func.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_NullDescription_AllowsNullDescription()
        {
            // ensure null description is allowed if implementation supports it
            var c = Concept.Create("id:thing", "Name", null);
            c.Should().NotBeNull();
            c.Description.Should().BeNull();
        }

        [Fact]
        public void CreateIdentifiable_ValidInputs_ReturnsIIdentifiable()
        {
            var gid = Guid.NewGuid();
            var ident = Concept.CreateIdentifiable(gid, "com.example:id", "Ident", "desc");
            Assert.NotNull(ident);
            Assert.Equal(gid, ident.GuidId);

            var asConcept = (IConcept)ident;
            var key = asConcept.Key;
            var displayName = asConcept.DisplayName;
            var description = asConcept.Description;
            key.Should().StartWith("com.example:id");
            displayName.Should().StartWith("Ident");
            description.Should().StartWith("desc");
        }

        [Fact]
        public void CreateIdentifiable_EmptyGuid_Throws()
        {
            var action = () => Concept.CreateIdentifiable(Guid.Empty, "id", "name", "desc");
            action.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public void CreateIdentifiable_InvalidIdOrName_Throws(string bad)
        {
            var action1 = () => Concept.CreateIdentifiable(Guid.NewGuid(), bad, "name");
            action1.Should().Throw<ArgumentException>();

            var action2 = () => Concept.CreateIdentifiable(Guid.NewGuid(), "id", bad);
            action2.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateIdentifiable_EmptyDescription_Throws()
        {
            var action = () => Concept.CreateIdentifiable(Guid.NewGuid(), "id", "name", "");
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_IdAndName_TrimmingBehavior()
        {
            // If implementation trims inputs, create with whitespace and verify normalized values.
            var c = Concept.Create("  com.example:trim  ", "  TrimName  ", "desc");

            c.Should().NotBeNull();
            c.Key.Should().Be("com.example:trim"); // tolerant check
            c.DisplayName.Should().Be("TrimName"); // tolerant check
            c.Description.Should().Be("desc"); // tolerant check
        }
    }
}
