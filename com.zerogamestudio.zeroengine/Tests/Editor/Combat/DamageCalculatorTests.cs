using NUnit.Framework;
using ZeroEngine.Combat;

namespace ZeroEngine.Tests.Combat
{
    /// <summary>
    /// DamageCalculator 单元测试
    /// </summary>
    [TestFixture]
    public class DamageCalculatorTests
    {
        private DamageCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new DamageCalculator();
        }

        #region Basic Damage Tests

        [Test]
        public void Calculate_NullTarget_ReturnsZeroDamage()
        {
            // Arrange
            var damage = DamageData.Physical(100f);

            // Act
            var result = _calculator.Calculate(damage, null);

            // Assert
            Assert.AreEqual(0f, result.FinalDamage);
        }

        [Test]
        public void Calculate_BasicDamage_ReturnsCorrectValue()
        {
            // Arrange
            var damage = DamageData.Physical(100f);
            var target = new MockCombatant();

            // Act
            var result = _calculator.Calculate(damage, target);

            // Assert
            Assert.AreEqual(100f, result.FinalDamage);
            Assert.IsFalse(result.IsCritical);
            Assert.IsFalse(result.IsDodged);
        }

        #endregion

        #region Armor Reduction Tests

        [Test]
        public void Calculate_WithArmor_ReducesDamage()
        {
            // 护甲公式: reduction = armor / (armor + 100)
            // 100 armor => 100/(100+100) = 0.5 => 50% 减免
            // 100 damage * (1 - 0.5) = 50

            // Arrange
            var damage = DamageData.Physical(100f);
            var target = new MockCombatant();

            // Act
            var result = _calculator.Calculate(
                damage,
                target,
                defenderStatGetter: stat => stat == "Armor" ? 100f : 0f
            );

            // Assert
            Assert.AreEqual(50f, result.FinalDamage);
        }

        [Test]
        public void Calculate_IgnoreArmor_BypassesReduction()
        {
            // Arrange
            var damage = DamageData.Physical(100f).WithFlags(DamageFlags.IgnoreArmor);
            var target = new MockCombatant();

            // Act
            var result = _calculator.Calculate(
                damage,
                target,
                defenderStatGetter: stat => stat == "Armor" ? 100f : 0f
            );

            // Assert
            Assert.AreEqual(100f, result.FinalDamage);
        }

        #endregion

        #region Damage Type Tests

        [Test]
        public void Calculate_MagicalDamage_UsesMagicResist()
        {
            // Arrange
            var damage = DamageData.Magical(100f, DamageType.Fire);
            var target = new MockCombatant();

            // Act
            var result = _calculator.Calculate(
                damage,
                target,
                defenderStatGetter: stat => stat == "MagicResist" ? 100f : 0f
            );

            // Assert
            Assert.AreEqual(50f, result.FinalDamage);
        }

        [Test]
        public void Calculate_TrueDamage_IgnoresAllDefense()
        {
            // Arrange
            var damage = DamageData.True(100f);
            var target = new MockCombatant();

            // Act
            var result = _calculator.Calculate(
                damage,
                target,
                defenderStatGetter: _ => 1000f // 高防御
            );

            // Assert - True damage 不受护甲/魔抗影响
            Assert.AreEqual(100f, result.FinalDamage);
        }

        #endregion

        #region DamageResult Tests

        [Test]
        public void DamageResult_Immune_HasCorrectFlags()
        {
            // Arrange
            var damage = DamageData.Physical(100f);

            // Act
            var result = DamageResult.Immune(damage);

            // Assert
            Assert.IsTrue(result.IsImmune);
            Assert.AreEqual(0f, result.FinalDamage);
        }

        [Test]
        public void DamageResult_Dodged_HasCorrectFlags()
        {
            // Arrange
            var damage = DamageData.Physical(100f);

            // Act
            var result = DamageResult.Dodged(damage);

            // Assert
            Assert.IsTrue(result.IsDodged);
            Assert.AreEqual(0f, result.FinalDamage);
        }

        #endregion

        #region Processor Tests

        [Test]
        public void RegisterProcessor_AddsToProcessorList()
        {
            // Arrange
            var processor = new MockDamageProcessor();

            // Act
            _calculator.RegisterProcessor(processor);
            var damage = DamageData.Physical(100f);
            var result = _calculator.Calculate(damage, new MockCombatant());

            // Assert - processor 应该被调用
            Assert.IsTrue(processor.WasCalled);
        }

        [Test]
        public void UnregisterProcessor_RemovesFromList()
        {
            // Arrange
            var processor = new MockDamageProcessor();
            _calculator.RegisterProcessor(processor);

            // Act
            _calculator.UnregisterProcessor(processor);
            var damage = DamageData.Physical(100f);
            _calculator.Calculate(damage, new MockCombatant());

            // Assert
            Assert.IsFalse(processor.WasCalled);
        }

        #endregion

        #region DamageData Tests

        [Test]
        public void DamageData_HasFlag_ReturnsCorrectly()
        {
            // Arrange
            var damage = DamageData.Physical(100f)
                .WithFlags(DamageFlags.IgnoreArmor)
                .WithFlags(DamageFlags.Lifesteal);

            // Assert
            Assert.IsTrue(damage.HasFlag(DamageFlags.IgnoreArmor));
            Assert.IsTrue(damage.HasFlag(DamageFlags.Lifesteal));
            Assert.IsFalse(damage.HasFlag(DamageFlags.IgnoreDodge));
        }

        [Test]
        public void DamageData_HasDamageType_ReturnsCorrectly()
        {
            // Arrange
            var damage = DamageData.Magical(100f, DamageType.Fire);

            // Assert
            Assert.IsTrue(damage.HasDamageType(DamageType.Magical));
            Assert.IsTrue(damage.HasDamageType(DamageType.Fire));
            Assert.IsFalse(damage.HasDamageType(DamageType.Physical));
        }

        [Test]
        public void DamageData_Physical_CreatesCorrectType()
        {
            // Act
            var damage = DamageData.Physical(50f);

            // Assert
            Assert.AreEqual(50f, damage.BaseDamage);
            Assert.IsTrue(damage.HasDamageType(DamageType.Physical));
        }

        [Test]
        public void DamageData_True_IgnoresArmor()
        {
            // Act
            var damage = DamageData.True(75f);

            // Assert
            Assert.AreEqual(75f, damage.BaseDamage);
            Assert.IsTrue(damage.HasFlag(DamageFlags.IgnoreArmor));
        }

        #endregion
    }

    #region Mock Classes

    /// <summary>
    /// 测试用 ICombatant 实现
    /// </summary>
    public class MockCombatant : ICombatant
    {
        public string CombatantId => "mock_001";
        public string DisplayName => "MockCombatant";
        public int TeamId => 1;
        public bool IsAlive => true;
        public bool IsTargetable => true;
        public UnityEngine.GameObject GameObject => null;
        public UnityEngine.Transform Transform => null;

        public UnityEngine.Vector3 GetCombatPosition() => UnityEngine.Vector3.zero;

        public DamageResult TakeDamage(DamageData damage)
        {
            return new DamageResult(damage, damage.BaseDamage);
        }

        public float ReceiveHeal(float amount, ICombatant source = null)
        {
            return amount;
        }

        public void OnEnterCombat() { }
        public void OnExitCombat() { }
    }

    /// <summary>
    /// 测试用伤害处理器
    /// </summary>
    public class MockDamageProcessor : IDamageProcessor
    {
        public int Priority => 0;
        public bool WasCalled { get; private set; }

        public DamageData ProcessDamage(DamageData damage, ICombatant target, DamageCalculationContext context)
        {
            WasCalled = true;
            return damage;
        }
    }

    #endregion
}
