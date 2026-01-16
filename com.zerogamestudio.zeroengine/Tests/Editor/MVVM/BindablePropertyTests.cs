using NUnit.Framework;
using ZeroEngine.UI.MVVM;

namespace ZeroEngine.Tests.MVVM
{
    [TestFixture]
    public class BindablePropertyTests
    {
        #region Basic Value Operations

        [Test]
        public void Value_SetAndGet()
        {
            var property = new BindableProperty<int>(10);
            Assert.AreEqual(10, property.Value);

            property.Value = 20;
            Assert.AreEqual(20, property.Value);
        }

        [Test]
        public void Value_SameValue_NoNotification()
        {
            var property = new BindableProperty<int>(10);
            var counter = TestHelpers.CreateEventCounter();
            property.OnValueChanged += counter.Increment;

            property.Value = 10; // Same value

            counter.AssertCount(0);
        }

        [Test]
        public void Value_DifferentValue_TriggersNotification()
        {
            var property = new BindableProperty<int>(10);
            var counter = TestHelpers.CreateEventCounter();
            property.OnValueChanged += counter.Increment;

            property.Value = 20;

            counter.AssertCount(1);
        }

        [Test]
        public void ImplicitConversion_Works()
        {
            var property = new BindableProperty<int>(42);
            int value = property;
            Assert.AreEqual(42, value);
        }

        #endregion

        #region Registration

        [Test]
        public void Register_ReceivesNotifications()
        {
            var property = new BindableProperty<int>(10);
            int receivedValue = 0;
            property.Register(v => receivedValue = v);

            property.Value = 20;

            Assert.AreEqual(20, receivedValue);
        }

        [Test]
        public void RegisterAndInvoke_ImmediatelyInvokes()
        {
            var property = new BindableProperty<int>(10);
            int receivedValue = 0;
            property.RegisterAndInvoke(v => receivedValue = v);

            Assert.AreEqual(10, receivedValue);
        }

        [Test]
        public void Unregister_StopsNotifications()
        {
            var property = new BindableProperty<int>(10);
            var counter = TestHelpers.CreateEventCounter();
            property.Register(counter.Increment);

            property.Value = 20;
            counter.AssertCount(1);

            property.Unregister(counter.Increment);
            property.Value = 30;
            counter.AssertCount(1); // Still 1, not incremented
        }

        [Test]
        public void ClearListeners_RemovesAll()
        {
            var property = new BindableProperty<int>(10);
            var counter1 = TestHelpers.CreateEventCounter();
            var counter2 = TestHelpers.CreateEventCounter();
            property.Register(counter1.Increment);
            property.Register(counter2.Increment);

            property.ClearListeners();
            property.Value = 20;

            counter1.AssertCount(0);
            counter2.AssertCount(0);
        }

        [Test]
        public void NotifyValueChanged_ForcesNotification()
        {
            var property = new BindableProperty<int>(10);
            var counter = TestHelpers.CreateEventCounter();
            property.Register(counter.Increment);

            property.NotifyValueChanged();

            counter.AssertCount(1);
        }

        #endregion

        #region Formatting

        [Test]
        public void WithFormat_Function_FormatsValue()
        {
            var property = new BindableProperty<int>(42)
                .WithFormat(v => $"Value: {v}");

            Assert.AreEqual("Value: 42", property.FormattedValue);
        }

        [Test]
        public void WithFormat_String_FormatsValue()
        {
            var property = new BindableProperty<float>(3.14159f)
                .WithFormat("{0:F2}");

            Assert.AreEqual("3.14", property.FormattedValue);
        }

        [Test]
        public void FormattedValue_NoFormatter_ReturnsToString()
        {
            var property = new BindableProperty<int>(42);
            Assert.AreEqual("42", property.FormattedValue);
        }

        [Test]
        public void FormattedValue_NullValue_ReturnsEmpty()
        {
            var property = new BindableProperty<string>(null);
            Assert.AreEqual(string.Empty, property.FormattedValue);
        }

        #endregion

        #region Validation

        [Test]
        public void WithValidation_Valid_SetsValue()
        {
            var property = new BindableProperty<int>(0)
                .WithValidation(v => v > 0, "Must be positive");

            property.Value = 10;

            Assert.AreEqual(10, property.Value);
            Assert.IsTrue(property.IsValid);
        }

        [Test]
        public void WithValidation_Invalid_RejectsValue()
        {
            var property = new BindableProperty<int>(10)
                .WithValidation(v => v > 0, "Must be positive");

            property.Value = -5;

            Assert.AreEqual(10, property.Value); // Value unchanged
            Assert.IsFalse(property.IsValid);
            Assert.AreEqual("Must be positive", property.ValidationState.ErrorMessage);
        }

        [Test]
        public void WithValidation_Function_Works()
        {
            var property = new BindableProperty<int>(0)
                .WithValidation(v => v >= 0 ? ValidationResult.Valid : ValidationResult.Invalid("Negative!"));

            property.Value = 5;
            Assert.IsTrue(property.IsValid);

            property.Value = -1;
            Assert.IsFalse(property.IsValid);
        }

        [Test]
        public void OnValidationChanged_Fires()
        {
            var property = new BindableProperty<int>(10)
                .WithValidation(v => v > 0, "Must be positive");

            var counter = TestHelpers.CreateEventCounter();
            property.OnValidationChanged += _ => counter.Increment();

            property.Value = 20;
            counter.AssertCount(1);

            property.Value = -5;
            counter.AssertCount(2);
        }

        [Test]
        public void SetValueWithoutValidation_BypassesValidation()
        {
            var property = new BindableProperty<int>(10)
                .WithValidation(v => v > 0, "Must be positive");

            property.SetValueWithoutValidation(-5);

            Assert.AreEqual(-5, property.Value);
        }

        #endregion
    }
}
