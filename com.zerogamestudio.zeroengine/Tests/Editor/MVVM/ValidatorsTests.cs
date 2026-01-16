using NUnit.Framework;
using ZeroEngine.UI.MVVM;

namespace ZeroEngine.Tests.MVVM
{
    [TestFixture]
    public class ValidatorsTests
    {
        #region String Validators

        [Test]
        public void NotEmpty_EmptyString_Invalid()
        {
            var validator = Validators.NotEmpty();
            Assert.IsFalse(validator("").IsValid);
            Assert.IsFalse(validator("   ").IsValid);
            Assert.IsFalse(validator(null).IsValid);
        }

        [Test]
        public void NotEmpty_NonEmptyString_Valid()
        {
            var validator = Validators.NotEmpty();
            Assert.IsTrue(validator("hello").IsValid);
        }

        [Test]
        public void MinLength_TooShort_Invalid()
        {
            var validator = Validators.MinLength(5);
            Assert.IsFalse(validator("abc").IsValid);
            Assert.IsFalse(validator(null).IsValid);
        }

        [Test]
        public void MinLength_LongEnough_Valid()
        {
            var validator = Validators.MinLength(5);
            Assert.IsTrue(validator("hello").IsValid);
            Assert.IsTrue(validator("hello world").IsValid);
        }

        [Test]
        public void MaxLength_TooLong_Invalid()
        {
            var validator = Validators.MaxLength(5);
            Assert.IsFalse(validator("hello world").IsValid);
        }

        [Test]
        public void MaxLength_ShortEnough_Valid()
        {
            var validator = Validators.MaxLength(5);
            Assert.IsTrue(validator("hello").IsValid);
            Assert.IsTrue(validator("hi").IsValid);
            Assert.IsTrue(validator(null).IsValid);
        }

        [Test]
        public void LengthRange_OutOfRange_Invalid()
        {
            var validator = Validators.LengthRange(3, 5);
            Assert.IsFalse(validator("ab").IsValid);
            Assert.IsFalse(validator("abcdef").IsValid);
        }

        [Test]
        public void LengthRange_InRange_Valid()
        {
            var validator = Validators.LengthRange(3, 5);
            Assert.IsTrue(validator("abc").IsValid);
            Assert.IsTrue(validator("abcd").IsValid);
            Assert.IsTrue(validator("abcde").IsValid);
        }

        [Test]
        public void Email_InvalidFormat_Invalid()
        {
            var validator = Validators.Email();
            Assert.IsFalse(validator("not-an-email").IsValid);
            Assert.IsFalse(validator("missing@domain").IsValid);
            Assert.IsFalse(validator("@example.com").IsValid);
        }

        [Test]
        public void Email_ValidFormat_Valid()
        {
            var validator = Validators.Email();
            Assert.IsTrue(validator("test@example.com").IsValid);
            Assert.IsTrue(validator("user.name@domain.org").IsValid);
        }

        [Test]
        public void Regex_NoMatch_Invalid()
        {
            var validator = Validators.Regex(@"^\d{4}$");
            Assert.IsFalse(validator("abc").IsValid);
            Assert.IsFalse(validator("12345").IsValid);
        }

        [Test]
        public void Regex_Match_Valid()
        {
            var validator = Validators.Regex(@"^\d{4}$");
            Assert.IsTrue(validator("1234").IsValid);
        }

        #endregion

        #region Numeric Validators

        [Test]
        public void IntRange_OutOfRange_Invalid()
        {
            var validator = Validators.IntRange(10, 20);
            Assert.IsFalse(validator(5).IsValid);
            Assert.IsFalse(validator(25).IsValid);
        }

        [Test]
        public void IntRange_InRange_Valid()
        {
            var validator = Validators.IntRange(10, 20);
            Assert.IsTrue(validator(10).IsValid);
            Assert.IsTrue(validator(15).IsValid);
            Assert.IsTrue(validator(20).IsValid);
        }

        [Test]
        public void FloatRange_OutOfRange_Invalid()
        {
            var validator = Validators.FloatRange(0.0f, 1.0f);
            Assert.IsFalse(validator(-0.1f).IsValid);
            Assert.IsFalse(validator(1.1f).IsValid);
        }

        [Test]
        public void FloatRange_InRange_Valid()
        {
            var validator = Validators.FloatRange(0.0f, 1.0f);
            Assert.IsTrue(validator(0.0f).IsValid);
            Assert.IsTrue(validator(0.5f).IsValid);
            Assert.IsTrue(validator(1.0f).IsValid);
        }

        [Test]
        public void Positive_ZeroOrNegative_Invalid()
        {
            var validator = Validators.Positive();
            Assert.IsFalse(validator(0).IsValid);
            Assert.IsFalse(validator(-1).IsValid);
        }

        [Test]
        public void Positive_PositiveValue_Valid()
        {
            var validator = Validators.Positive();
            Assert.IsTrue(validator(1).IsValid);
            Assert.IsTrue(validator(100).IsValid);
        }

        [Test]
        public void NonNegative_Negative_Invalid()
        {
            var validator = Validators.NonNegative();
            Assert.IsFalse(validator(-1).IsValid);
        }

        [Test]
        public void NonNegative_ZeroOrPositive_Valid()
        {
            var validator = Validators.NonNegative();
            Assert.IsTrue(validator(0).IsValid);
            Assert.IsTrue(validator(1).IsValid);
        }

        #endregion

        #region Composition

        [Test]
        public void All_AllPass_Valid()
        {
            var validator = Validators.All(
                Validators.NotEmpty(),
                Validators.MinLength(3),
                Validators.MaxLength(10)
            );
            Assert.IsTrue(validator("hello").IsValid);
        }

        [Test]
        public void All_OneFails_Invalid()
        {
            var validator = Validators.All(
                Validators.NotEmpty(),
                Validators.MinLength(10)
            );
            Assert.IsFalse(validator("hello").IsValid);
        }

        [Test]
        public void Any_OnePass_Valid()
        {
            var validator = Validators.Any(
                Validators.Email(),
                Validators.Regex(@"^\d{10}$")
            );
            Assert.IsTrue(validator("test@example.com").IsValid);
            Assert.IsTrue(validator("1234567890").IsValid);
        }

        [Test]
        public void Any_AllFail_Invalid()
        {
            var validator = Validators.Any(
                Validators.Email(),
                Validators.Regex(@"^\d{10}$")
            );
            Assert.IsFalse(validator("not-valid").IsValid);
        }

        #endregion
    }
}
