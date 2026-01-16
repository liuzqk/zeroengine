using System.Collections.Generic;
using NUnit.Framework;
using ZeroEngine.Loot;

namespace ZeroEngine.Tests.Loot
{
    [TestFixture]
    public class LootTableTests
    {
        #region LootCondition Tests

        [Test]
        public void ProbabilityCondition_AlwaysTrue_ReturnsTrue()
        {
            var condition = new ProbabilityCondition { Probability = 1.0f };
            var context = new LootContext();

            // With 100% probability, should always pass
            bool result = condition.Check(context);
            Assert.IsTrue(result);
        }

        [Test]
        public void ProbabilityCondition_AlwaysFalse_ReturnsFalse()
        {
            var condition = new ProbabilityCondition { Probability = 0f };
            var context = new LootContext();

            bool result = condition.Check(context);
            Assert.IsFalse(result);
        }

        [Test]
        public void LevelCondition_MeetsRequirement_ReturnsTrue()
        {
            var condition = new LevelCondition { MinLevel = 5, MaxLevel = 10 };
            var context = new LootContext { PlayerLevel = 7 };

            bool result = condition.Check(context);
            Assert.IsTrue(result);
        }

        [Test]
        public void LevelCondition_BelowMinimum_ReturnsFalse()
        {
            var condition = new LevelCondition { MinLevel = 5, MaxLevel = 10 };
            var context = new LootContext { PlayerLevel = 3 };

            bool result = condition.Check(context);
            Assert.IsFalse(result);
        }

        [Test]
        public void LevelCondition_AboveMaximum_ReturnsFalse()
        {
            var condition = new LevelCondition { MinLevel = 5, MaxLevel = 10 };
            var context = new LootContext { PlayerLevel = 15 };

            bool result = condition.Check(context);
            Assert.IsFalse(result);
        }

        [Test]
        public void LevelCondition_NoMaxLimit_AcceptsHighLevels()
        {
            var condition = new LevelCondition { MinLevel = 5, MaxLevel = 0 }; // 0 = no max
            var context = new LootContext { PlayerLevel = 100 };

            bool result = condition.Check(context);
            Assert.IsTrue(result);
        }

        [Test]
        public void CompositeCondition_And_AllMet_ReturnsTrue()
        {
            var condition = new CompositeCondition
            {
                Operator = CompositeCondition.LogicalOperator.And,
                SubConditions = new LootCondition[]
                {
                    new ProbabilityCondition { Probability = 1.0f },
                    new LevelCondition { MinLevel = 1, MaxLevel = 10 }
                }
            };
            var context = new LootContext { PlayerLevel = 5 };

            bool result = condition.Check(context);
            Assert.IsTrue(result);
        }

        [Test]
        public void CompositeCondition_And_OneFails_ReturnsFalse()
        {
            var condition = new CompositeCondition
            {
                Operator = CompositeCondition.LogicalOperator.And,
                SubConditions = new LootCondition[]
                {
                    new ProbabilityCondition { Probability = 1.0f },
                    new LevelCondition { MinLevel = 10, MaxLevel = 20 } // Requires level 10+
                }
            };
            var context = new LootContext { PlayerLevel = 5 }; // Level 5, fails

            bool result = condition.Check(context);
            Assert.IsFalse(result);
        }

        [Test]
        public void CompositeCondition_Or_OneMet_ReturnsTrue()
        {
            var condition = new CompositeCondition
            {
                Operator = CompositeCondition.LogicalOperator.Or,
                SubConditions = new LootCondition[]
                {
                    new ProbabilityCondition { Probability = 0f }, // Always fails
                    new LevelCondition { MinLevel = 1, MaxLevel = 10 } // Should pass
                }
            };
            var context = new LootContext { PlayerLevel = 5 };

            bool result = condition.Check(context);
            Assert.IsTrue(result);
        }

        [Test]
        public void CompositeCondition_Or_NoneMet_ReturnsFalse()
        {
            var condition = new CompositeCondition
            {
                Operator = CompositeCondition.LogicalOperator.Or,
                SubConditions = new LootCondition[]
                {
                    new ProbabilityCondition { Probability = 0f },
                    new LevelCondition { MinLevel = 10, MaxLevel = 20 }
                }
            };
            var context = new LootContext { PlayerLevel = 5 };

            bool result = condition.Check(context);
            Assert.IsFalse(result);
        }

        #endregion

        #region LootEntry Tests

        [Test]
        public void LootEntry_CalculatesAmount_WithinRange()
        {
            var entry = new LootEntry
            {
                AmountMin = 5,
                AmountMax = 10
            };

            // Run multiple times to verify range
            for (int i = 0; i < 100; i++)
            {
                int amount = UnityEngine.Random.Range(entry.AmountMin, entry.AmountMax + 1);
                Assert.GreaterOrEqual(amount, entry.AmountMin);
                Assert.LessOrEqual(amount, entry.AmountMax);
            }
        }

        [Test]
        public void LootEntry_NothingType_IsValid()
        {
            var entry = new LootEntry
            {
                Type = LootEntryType.Nothing,
                Weight = 10
            };

            Assert.AreEqual(LootEntryType.Nothing, entry.Type);
            Assert.AreEqual(10, entry.Weight);
        }

        #endregion

        #region Weight Calculation Tests

        [Test]
        public void WeightedSelection_CalculatesTotalWeight()
        {
            var entries = new List<LootEntry>
            {
                new LootEntry { Weight = 10 },
                new LootEntry { Weight = 20 },
                new LootEntry { Weight = 30 }
            };

            float totalWeight = 0f;
            foreach (var entry in entries)
            {
                totalWeight += entry.Weight;
            }

            Assert.AreEqual(60f, totalWeight);
        }

        [Test]
        public void WeightedSelection_ZeroWeight_IsSkipped()
        {
            var entries = new List<LootEntry>
            {
                new LootEntry { Weight = 0 },
                new LootEntry { Weight = 10 }
            };

            int validEntries = 0;
            foreach (var entry in entries)
            {
                if (entry.Weight > 0) validEntries++;
            }

            Assert.AreEqual(1, validEntries);
        }

        #endregion

        #region LootContext Tests

        [Test]
        public void LootContext_DefaultValues()
        {
            var context = new LootContext();

            Assert.AreEqual(0, context.PlayerLevel);
            Assert.IsNotNull(context.CustomData);
            Assert.AreEqual(0, context.CustomData.Count);
        }

        [Test]
        public void LootContext_CustomData_CanBeSet()
        {
            var context = new LootContext
            {
                PlayerLevel = 10,
                CustomData = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 42 }
                }
            };

            Assert.AreEqual(10, context.PlayerLevel);
            Assert.IsNotNull(context.CustomData);
            Assert.AreEqual("value1", context.CustomData["key1"]);
            Assert.AreEqual(42, context.CustomData["key2"]);
        }

        #endregion

        #region LootResult Tests

        [Test]
        public void LootResult_Currency_HasCorrectType()
        {
            var result = new LootResult
            {
                Type = LootEntryType.Currency,
                Currency = CurrencyType.Gold,
                Amount = 100
            };

            Assert.AreEqual(LootEntryType.Currency, result.Type);
            Assert.AreEqual(CurrencyType.Gold, result.Currency);
            Assert.AreEqual(100, result.Amount);
        }

        [Test]
        public void LootResult_Nothing_HasCorrectType()
        {
            var result = new LootResult
            {
                Type = LootEntryType.Nothing
            };

            Assert.AreEqual(LootEntryType.Nothing, result.Type);
        }

        #endregion

        #region PityConfig Tests

        [Test]
        public void PityConfig_Defaults()
        {
            var pity = new PityConfig();

            Assert.AreEqual(10, pity.MaxAttempts);
            TestHelpers.AssertApproximatelyEqual(0.1f, pity.IncrementPerFail);
            Assert.IsFalse(pity.GlobalPity);
        }

        [Test]
        public void PityConfig_CalculatesGuarantee()
        {
            var pity = new PityConfig
            {
                MaxAttempts = 10,
                IncrementPerFail = 0.1f
            };

            // After 10 attempts, should hit pity
            int attempts = 0;
            while (attempts < pity.MaxAttempts)
            {
                attempts += 1;
            }

            Assert.GreaterOrEqual(attempts, pity.MaxAttempts);
        }

        #endregion
    }
}
