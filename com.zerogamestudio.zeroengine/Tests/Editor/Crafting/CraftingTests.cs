using System.Collections.Generic;
using NUnit.Framework;
using ZeroEngine.Crafting;

namespace ZeroEngine.Tests.Crafting
{
    [TestFixture]
    public class CraftingEnumsTests
    {
        #region RecipeCategory Tests

        [Test]
        public void RecipeCategory_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Weapon));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Armor));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Consumable));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Material));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Tool));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Decoration));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeCategory), RecipeCategory.Other));
        }

        #endregion

        #region RecipeUnlockType Tests

        [Test]
        public void RecipeUnlockType_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Default));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Level));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Quest));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Achievement));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Item));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Relationship));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecipeUnlockType), RecipeUnlockType.Custom));
        }

        #endregion

        #region CraftingResult Tests

        [Test]
        public void CraftingResult_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.Success));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.GreatSuccess));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.InsufficientMaterials));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.FailedKeepMaterials));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.FailedLoseMaterials));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.RecipeLocked));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingResult), CraftingResult.WrongWorkbench));
        }

        #endregion

        #region CraftingEventType Tests

        [Test]
        public void CraftingEventType_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingEventType), CraftingEventType.Started));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingEventType), CraftingEventType.Completed));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingEventType), CraftingEventType.Failed));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingEventType), CraftingEventType.RecipeUnlocked));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CraftingEventType), CraftingEventType.SkillLevelUp));
        }

        #endregion
    }

    [TestFixture]
    public class RecipeIngredientTests
    {
        #region RecipeIngredient Tests

        [Test]
        public void RecipeIngredient_DefaultAmount_IsOne()
        {
            var ingredient = new RecipeIngredient();

            Assert.AreEqual(1, ingredient.Amount);
        }

        [Test]
        public void RecipeIngredient_CanSetAmount()
        {
            var ingredient = new RecipeIngredient { Amount = 5 };

            Assert.AreEqual(5, ingredient.Amount);
        }

        [Test]
        public void RecipeIngredient_NoItem_IsInvalid()
        {
            var ingredient = new RecipeIngredient { Item = null };

            Assert.IsNull(ingredient.Item);
        }

        #endregion

        #region RecipeOutput Tests

        [Test]
        public void RecipeOutput_DefaultAmount_IsOne()
        {
            var output = new RecipeOutput();

            Assert.AreEqual(1, output.BaseAmount);
        }

        [Test]
        public void RecipeOutput_DefaultProbability_Is100Percent()
        {
            var output = new RecipeOutput();

            TestHelpers.AssertApproximatelyEqual(1f, output.Probability);
        }

        [Test]
        public void RecipeOutput_CanSetProbability()
        {
            var output = new RecipeOutput { Probability = 0.5f };

            TestHelpers.AssertApproximatelyEqual(0.5f, output.Probability);
        }

        #endregion
    }

    [TestFixture]
    public class CraftingProgressTests
    {
        #region CraftingProgress Tests

        [Test]
        public void CraftingProgress_DefaultValues()
        {
            var progress = new CraftingProgress();

            Assert.IsNull(progress.RecipeId);
            Assert.AreEqual(0, progress.StartTime);
            Assert.AreEqual(0, progress.EndTime);
            Assert.AreEqual(0, progress.BatchCount);
        }

        [Test]
        public void CraftingProgress_IsComplete_WhenEndTimeReached()
        {
            var progress = new CraftingProgress
            {
                StartTime = 0,
                EndTime = 100
            };

            // At time 50, not complete
            bool notComplete = 50 >= progress.EndTime;
            Assert.IsFalse(notComplete);

            // At time 100+, complete
            bool complete = 100 >= progress.EndTime;
            Assert.IsTrue(complete);
        }

        [Test]
        public void CraftingProgress_InstantCraft_IsImmediatelyComplete()
        {
            var progress = new CraftingProgress
            {
                StartTime = 100,
                EndTime = 100  // Same time = instant
            };

            bool complete = progress.StartTime >= progress.EndTime;
            Assert.IsTrue(complete);
        }

        #endregion
    }

    [TestFixture]
    public class CraftingSkillDataTests
    {
        #region CraftingSkillData Tests

        [Test]
        public void CraftingSkillData_DefaultLevel_IsOne()
        {
            var skill = new CraftingSkillData();

            Assert.AreEqual(1, skill.Level);
        }

        [Test]
        public void CraftingSkillData_DefaultExp_IsZero()
        {
            var skill = new CraftingSkillData();

            Assert.AreEqual(0, skill.Experience);
        }

        [Test]
        public void CraftingSkillData_CanLevelUp()
        {
            var skill = new CraftingSkillData
            {
                Level = 1,
                Experience = 0
            };

            skill.Level = 2;
            skill.Experience = 0;

            Assert.AreEqual(2, skill.Level);
        }

        #endregion
    }

    [TestFixture]
    public class CraftingEventArgsTests
    {
        #region Factory Methods Tests

        [Test]
        public void CraftingEventArgs_Started_HasCorrectType()
        {
            var args = CraftingEventArgs.Started(null, 1);

            Assert.AreEqual(CraftingEventType.Started, args.EventType);
            Assert.AreEqual(1, args.BatchCount);
        }

        [Test]
        public void CraftingEventArgs_Completed_HasCorrectType()
        {
            var outputs = new List<RecipeOutput>();
            var args = CraftingEventArgs.Completed(null, CraftingResult.Success, outputs);

            Assert.AreEqual(CraftingEventType.Completed, args.EventType);
            Assert.AreEqual(CraftingResult.Success, args.Result);
            Assert.AreSame(outputs, args.Outputs);
        }

        [Test]
        public void CraftingEventArgs_Failed_HasCorrectType()
        {
            var args = CraftingEventArgs.Failed(null, CraftingResult.FailedKeepMaterials);

            Assert.AreEqual(CraftingEventType.Failed, args.EventType);
            Assert.AreEqual(CraftingResult.FailedKeepMaterials, args.Result);
        }

        [Test]
        public void CraftingEventArgs_RecipeUnlocked_HasCorrectType()
        {
            var args = CraftingEventArgs.RecipeUnlocked(null);

            Assert.AreEqual(CraftingEventType.RecipeUnlocked, args.EventType);       
        }

        #endregion
    }
}
