using NUnit.Framework;
using ZeroEngine.StatSystem;

namespace ZeroEngine.Tests.Data
{
    /// <summary>
    /// Stat 和 StatModifier 单元测试
    /// </summary>
    [TestFixture]
    public class StatSystemTests
    {
        #region Stat Basic Tests

        [Test]
        public void Stat_Constructor_SetsBaseValue()
        {
            // Arrange & Act
            var stat = new Stat(100f);

            // Assert
            Assert.AreEqual(100f, stat.BaseValue);
            Assert.AreEqual(100f, stat.Value);
        }

        [Test]
        public void Stat_ConstructorWithMinMax_SetsLimits()
        {
            // Arrange & Act
            var stat = new Stat(50f, 0f, 100f);

            // Assert
            Assert.AreEqual(50f, stat.Value);
            Assert.AreEqual(0f, stat.MinValue);
            Assert.AreEqual(100f, stat.MaxValue);
        }

        [Test]
        public void Stat_InitBase_UpdatesBaseValue()
        {
            // Arrange
            var stat = new Stat(100f);

            // Act
            stat.InitBase(200f);

            // Assert
            Assert.AreEqual(200f, stat.BaseValue);
            Assert.AreEqual(200f, stat.Value);
        }

        #endregion

        #region Modifier Tests

        [Test]
        public void AddModifier_Flat_IncreasesValue()
        {
            // Arrange
            var stat = new Stat(100f);
            var modifier = new StatModifier(50f, StatModType.Flat);

            // Act
            stat.AddModifier(modifier);

            // Assert
            Assert.AreEqual(150f, stat.Value);
        }

        [Test]
        public void AddModifier_PercentAdd_AppliesPercentage()
        {
            // Arrange
            var stat = new Stat(100f);
            var modifier = new StatModifier(0.5f, StatModType.PercentAdd); // +50%

            // Act
            stat.AddModifier(modifier);

            // Assert
            Assert.AreEqual(150f, stat.Value);
        }

        [Test]
        public void AddModifier_PercentMult_MultipliesValue()
        {
            // Arrange
            var stat = new Stat(100f);
            var modifier = new StatModifier(2f, StatModType.PercentMult); // x2

            // Act
            stat.AddModifier(modifier);

            // Assert
            Assert.AreEqual(200f, stat.Value);
        }

        [Test]
        public void AddModifier_Combined_CalculatesCorrectly()
        {
            // 公式: (Base + Flat) * (1 + PercentAdd) * PercentMult
            // (100 + 50) * (1 + 0.5) * 2 = 150 * 1.5 * 2 = 450

            // Arrange
            var stat = new Stat(100f);

            // Act
            stat.AddModifier(new StatModifier(50f, StatModType.Flat));
            stat.AddModifier(new StatModifier(0.5f, StatModType.PercentAdd));
            stat.AddModifier(new StatModifier(2f, StatModType.PercentMult));

            // Assert
            Assert.AreEqual(450f, stat.Value);
        }

        [Test]
        public void RemoveModifier_RestoresOriginalValue()
        {
            // Arrange
            var stat = new Stat(100f);
            var modifier = new StatModifier(50f, StatModType.Flat);
            stat.AddModifier(modifier);

            // Act
            bool removed = stat.RemoveModifier(modifier);

            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(100f, stat.Value);
        }

        [Test]
        public void RemoveAllModifiersFromSource_RemovesCorrectModifiers()
        {
            // Arrange
            var stat = new Stat(100f);
            var source1 = new object();
            var source2 = new object();

            stat.AddModifier(new StatModifier(10f, StatModType.Flat), source1);
            stat.AddModifier(new StatModifier(20f, StatModType.Flat), source1);
            stat.AddModifier(new StatModifier(30f, StatModType.Flat), source2);

            // Act
            stat.RemoveAllModifiersFromSource(source1);

            // Assert
            Assert.AreEqual(1, stat.ModifierCount);
            Assert.AreEqual(130f, stat.Value); // 100 + 30
        }

        #endregion

        #region Min/Max Clamp Tests

        [Test]
        public void Stat_Value_ClampedToMin()
        {
            // Arrange
            var stat = new Stat(100f, 0f, 200f);
            var modifier = new StatModifier(-150f, StatModType.Flat);

            // Act
            stat.AddModifier(modifier);

            // Assert
            Assert.AreEqual(0f, stat.Value);
        }

        [Test]
        public void Stat_Value_ClampedToMax()
        {
            // Arrange
            var stat = new Stat(100f, 0f, 150f);
            var modifier = new StatModifier(100f, StatModType.Flat);

            // Act
            stat.AddModifier(modifier);

            // Assert
            Assert.AreEqual(150f, stat.Value);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnValueChanged_FiresWhenValueChanges()
        {
            // Arrange
            var stat = new Stat(100f);
            StatChangedEventArgs? eventArgs = null;
            stat.OnValueChanged += args => eventArgs = args;

            // Act
            stat.AddModifier(new StatModifier(50f, StatModType.Flat));
            _ = stat.Value; // 触发计算

            // Assert
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(100f, eventArgs.Value.OldValue);
            Assert.AreEqual(150f, eventArgs.Value.NewValue);
            Assert.AreEqual(50f, eventArgs.Value.Delta);
        }

        [Test]
        public void ClearEventListeners_RemovesAllListeners()
        {
            // Arrange
            var stat = new Stat(100f);
            int callCount = 0;
            stat.OnValueChanged += _ => callCount++;

            // Act
            stat.ClearEventListeners();
            stat.AddModifier(new StatModifier(50f, StatModType.Flat));
            _ = stat.Value;

            // Assert
            Assert.AreEqual(0, callCount);
        }

        #endregion

        #region StatModifier Tests

        [Test]
        public void StatModifier_Constructor_SetsProperties()
        {
            // Arrange & Act
            var source = new object();
            var modifier = new StatModifier(10f, StatModType.Flat, 50, source);

            // Assert
            Assert.AreEqual(10f, modifier.Value);
            Assert.AreEqual(StatModType.Flat, modifier.ModType);
            Assert.AreEqual(50, modifier.Order);
            Assert.AreSame(source, modifier.Source);
        }

        [Test]
        public void StatModifier_GetValue_ReturnsValue()
        {
            // Arrange
            var modifier = new StatModifier(25f, StatModType.Flat);

            // Act & Assert
            Assert.AreEqual(25f, modifier.GetValue());
        }

        #endregion

        #region Multiple PercentAdd Tests

        [Test]
        public void MultiplePercentAdd_SumsAdditively()
        {
            // 多个 PercentAdd 应该相加：(1 + 0.2 + 0.3) = 1.5
            // 100 * 1.5 = 150

            // Arrange
            var stat = new Stat(100f);

            // Act
            stat.AddModifier(new StatModifier(0.2f, StatModType.PercentAdd));
            stat.AddModifier(new StatModifier(0.3f, StatModType.PercentAdd));

            // Assert
            Assert.AreEqual(150f, stat.Value);
        }

        [Test]
        public void MultiplePercentMult_MultipliesSequentially()
        {
            // 多个 PercentMult 应该相乘：1.5 * 2 = 3
            // 100 * 3 = 300

            // Arrange
            var stat = new Stat(100f);

            // Act
            stat.AddModifier(new StatModifier(1.5f, StatModType.PercentMult));
            stat.AddModifier(new StatModifier(2f, StatModType.PercentMult));

            // Assert
            Assert.AreEqual(300f, stat.Value);
        }

        #endregion
    }
}
