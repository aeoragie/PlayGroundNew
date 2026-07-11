using PlayGround.Shared.Http;
using PlayGround.Shared.Primitives;
using PlayGround.Shared.Text;
using Xunit;

namespace PlayGround.Tests.Unit.Core
{
    public class PagedDataTests
    {
        [Fact]
        public void TotalPages_NormalSize_CalculatesCeiling()
        {
            var paged = new PagedData<int>([], totalCount: 25, page: 1, size: 10);

            Assert.Equal(3, paged.TotalPages);
        }

        [Fact]
        public void TotalPages_ZeroSize_ReturnsZeroInsteadOfCrash()
        {
            var paged = new PagedData<int>([], totalCount: 25, page: 1, size: 0);

            Assert.Equal(0, paged.TotalPages);
        }
    }

    public class ValueObjectTests
    {
        private class Money : ValueObject
        {
            public decimal Amount { get; }
            public string Currency { get; }

            public Money(decimal amount, string currency)
            {
                Amount = amount;
                Currency = currency;
            }

            protected override IEnumerable<object?> GetEqualityComponents()
            {
                yield return Amount;
                yield return Currency;
            }
        }

        private class EmptyValueObject : ValueObject
        {
            protected override IEnumerable<object?> GetEqualityComponents()
            {
                yield break;
            }
        }

        [Fact]
        public void Equals_SameComponents_ReturnsTrue()
        {
            Assert.Equal(new Money(100m, "KRW"), new Money(100m, "KRW"));
            Assert.True(new Money(100m, "KRW") == new Money(100m, "KRW"));
        }

        [Fact]
        public void Equals_DifferentComponents_ReturnsFalse()
        {
            Assert.NotEqual(new Money(100m, "KRW"), new Money(100m, "USD"));
        }

        [Fact]
        public void GetHashCode_EmptyComponents_DoesNotThrow()
        {
            var hash = new EmptyValueObject().GetHashCode();

            Assert.Equal(new EmptyValueObject().GetHashCode(), hash);
        }

        [Fact]
        public void GetHashCode_ComponentOrder_Matters()
        {
            Assert.NotEqual(
                new Money(1m, "2").GetHashCode(),
                new Money(2m, "1").GetHashCode());
        }
    }

    public class SlugGeneratorTests
    {
        [Fact]
        public void Generate_Hangul_RomanizesPerSyllable()
        {
            Assert.Equal("sonheungmin", SlugGenerator.Generate("손흥민"));
        }

        [Fact]
        public void Generate_MixedText_JoinsWithHyphen()
        {
            Assert.Equal("fc-seoul-u15", SlugGenerator.Generate("FC Seoul U15"));
        }

        [Fact]
        public void Generate_EmptyInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, SlugGenerator.Generate("  "));
            Assert.Equal(string.Empty, SlugGenerator.Generate(null));
        }

        [Fact]
        public void MakeUnique_Collision_AppendsSuffix()
        {
            var taken = new HashSet<string> { "fc-seoul", "fc-seoul-2" };

            Assert.Equal("fc-seoul-3", SlugGenerator.MakeUnique("fc-seoul", taken.Contains));
        }

        [Fact]
        public void MakeUnique_EmptyBase_UsesFallback()
        {
            Assert.Equal("item", SlugGenerator.MakeUnique(null, _ => false));
        }
    }
}
