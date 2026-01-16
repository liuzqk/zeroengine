using System.Collections.Generic;
using NUnit.Framework;
using ZeroEngine.Relationship;

namespace ZeroEngine.Tests.Relationship
{
    [TestFixture]
    public class RelationshipEnumsTests
    {
        #region RelationshipLevel Tests

        [Test]
        public void RelationshipLevel_HasCorrectOrder()
        {
            Assert.Less((int)RelationshipLevel.Stranger, (int)RelationshipLevel.Acquaintance);
            Assert.Less((int)RelationshipLevel.Acquaintance, (int)RelationshipLevel.Friend);
            Assert.Less((int)RelationshipLevel.Friend, (int)RelationshipLevel.CloseFriend);
            Assert.Less((int)RelationshipLevel.CloseFriend, (int)RelationshipLevel.BestFriend);
            Assert.Less((int)RelationshipLevel.BestFriend, (int)RelationshipLevel.Lover);
            Assert.Less((int)RelationshipLevel.Lover, (int)RelationshipLevel.Partner);
        }

        [Test]
        public void RelationshipLevel_Stranger_IsLowest()
        {
            Assert.AreEqual(0, (int)RelationshipLevel.Stranger);
        }

        [Test]
        public void RelationshipLevel_Partner_IsHighest()
        {
            int maxValue = 0;
            foreach (RelationshipLevel level in System.Enum.GetValues(typeof(RelationshipLevel)))
            {
                if ((int)level > maxValue) maxValue = (int)level;
            }
            Assert.AreEqual((int)RelationshipLevel.Partner, maxValue);
        }

        #endregion

        #region GiftPreference Tests

        [Test]
        public void GiftPreference_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(GiftPreference), GiftPreference.Loved));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GiftPreference), GiftPreference.Liked));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GiftPreference), GiftPreference.Neutral));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GiftPreference), GiftPreference.Disliked));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GiftPreference), GiftPreference.Hated));
        }

        [Test]
        public void GiftPreference_Loved_IsPositive()
        {
            // Loved should give more points than Liked
            Assert.Greater((int)GiftPreference.Loved, (int)GiftPreference.Liked);
        }

        #endregion

        #region NpcType Tests

        [Test]
        public void NpcType_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(NpcType), NpcType.Normal));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NpcType), NpcType.Romanceable));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NpcType), NpcType.Merchant));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NpcType), NpcType.Quest));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NpcType), NpcType.Companion));
        }

        #endregion

        #region RelationshipEventType Tests

        [Test]
        public void RelationshipEventType_HasExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.PointsChanged));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.LevelUp));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.LevelDown));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.GiftReceived));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.DialogueCompleted));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelationshipEventType), RelationshipEventType.SpecialEvent));
        }

        #endregion
    }

    [TestFixture]
    public class RelationshipProgressTests
    {
        #region RelationshipProgress Tests

        [Test]
        public void RelationshipProgress_DefaultLevel_IsStranger()
        {
            var progress = new RelationshipProgress();

            Assert.AreEqual(RelationshipLevel.Stranger, progress.Level);
        }

        [Test]
        public void RelationshipProgress_DefaultPoints_IsZero()
        {
            var progress = new RelationshipProgress();

            Assert.AreEqual(0, progress.Points);
        }

        [Test]
        public void RelationshipProgress_DefaultGiftCount_IsZero()
        {
            var progress = new RelationshipProgress();

            Assert.AreEqual(0, progress.GiftCountToday);
        }

        [Test]
        public void RelationshipProgress_DefaultTalkCount_IsZero()
        {
            var progress = new RelationshipProgress();

            Assert.AreEqual(0, progress.TalkCountToday);
        }

        [Test]
        public void RelationshipProgress_TriggeredEvents_IsInitialized()
        {
            var progress = new RelationshipProgress();

            Assert.IsNotNull(progress.TriggeredEvents);
        }

        [Test]
        public void RelationshipProgress_CustomData_IsInitialized()
        {
            var progress = new RelationshipProgress();

            Assert.IsNotNull(progress.CustomData);
        }

        [Test]
        public void RelationshipProgress_CanTrackTriggeredEvents()
        {
            var progress = new RelationshipProgress();

            progress.TriggeredEvents.Add("event1");
            progress.TriggeredEvents.Add("event2");

            Assert.AreEqual(2, progress.TriggeredEvents.Count);
            Assert.IsTrue(progress.TriggeredEvents.Contains("event1"));
            Assert.IsTrue(progress.TriggeredEvents.Contains("event2"));
        }

        [Test]
        public void RelationshipProgress_CanStoreCustomData()
        {
            var progress = new RelationshipProgress();

            progress.CustomData["key1"] = "value1";
            progress.CustomData["key2"] = "value2";

            Assert.AreEqual("value1", progress.CustomData["key1"]);
            Assert.AreEqual("value2", progress.CustomData["key2"]);
        }

        #endregion
    }

    [TestFixture]
    public class GiftDataTests
    {
        #region GiftData Tests

        [Test]
        public void GiftData_DefaultPoints_IsZero()
        {
            var gift = new GiftData();

            Assert.AreEqual(10, gift.PointsChange);
        }

        [Test]
        public void GiftData_CanSetPoints()
        {
            var gift = new GiftData { PointsChange = 50 };

            Assert.AreEqual(50, gift.PointsChange);
        }

        [Test]
        public void GiftData_CanBeNegative()
        {
            var gift = new GiftData { PointsChange = -30 };

            Assert.AreEqual(-30, gift.PointsChange);
        }

        #endregion
    }

    [TestFixture]
    public class RelationshipThresholdTests
    {
        #region RelationshipThreshold Tests

        [Test]
        public void RelationshipThreshold_DefaultRequiredPoints_IsZero()
        {
            var threshold = new RelationshipThreshold();

            Assert.AreEqual(0, threshold.RequiredPoints);
        }

        [Test]
        public void RelationshipThreshold_CanSetLevel()
        {
            var threshold = new RelationshipThreshold
            {
                Level = RelationshipLevel.Friend,
                RequiredPoints = 100
            };

            Assert.AreEqual(RelationshipLevel.Friend, threshold.Level);
            Assert.AreEqual(100, threshold.RequiredPoints);
        }

        [Test]
        public void RelationshipThreshold_Comparison()
        {
            var threshold1 = new RelationshipThreshold
            {
                Level = RelationshipLevel.Friend,
                RequiredPoints = 100
            };

            var threshold2 = new RelationshipThreshold
            {
                Level = RelationshipLevel.BestFriend,
                RequiredPoints = 500
            };

            Assert.Less(threshold1.RequiredPoints, threshold2.RequiredPoints);
        }

        #endregion
    }

    [TestFixture]
    public class RelationshipEventArgsTests
    {
        #region Factory Methods Tests

        [Test]
        public void RelationshipEventArgs_PointsChanged_HasCorrectType()
        {
            var args = RelationshipEventArgs.PointsChanged("npc1", "NPC Name", 50, 75);

            Assert.AreEqual(RelationshipEventType.PointsChanged, args.EventType);
            Assert.AreEqual("npc1", args.NpcId);
            Assert.AreEqual("NPC Name", args.NpcName);
            Assert.AreEqual(50, args.OldPoints);
            Assert.AreEqual(75, args.NewPoints);
        }

        [Test]
        public void RelationshipEventArgs_LevelUp_HasCorrectType()
        {
            var args = RelationshipEventArgs.LevelUp("npc1", "NPC Name",
                RelationshipLevel.Friend, RelationshipLevel.CloseFriend);

            Assert.AreEqual(RelationshipEventType.LevelUp, args.EventType);
            Assert.AreEqual(RelationshipLevel.Friend, args.OldLevel);
            Assert.AreEqual(RelationshipLevel.CloseFriend, args.NewLevel);
        }

        [Test]
        public void RelationshipEventArgs_LevelDown_HasCorrectType()
        {
            var args = RelationshipEventArgs.LevelDown("npc1", "NPC Name",
                RelationshipLevel.CloseFriend, RelationshipLevel.Friend);

            Assert.AreEqual(RelationshipEventType.LevelDown, args.EventType);
            Assert.AreEqual(RelationshipLevel.CloseFriend, args.OldLevel);
            Assert.AreEqual(RelationshipLevel.Friend, args.NewLevel);
        }

        [Test]
        public void RelationshipEventArgs_GiftReceived_HasCorrectType()
        {
            var args = RelationshipEventArgs.GiftReceived("npc1", "NPC Name",
                null, GiftPreference.Liked, 25);

            Assert.AreEqual(RelationshipEventType.GiftReceived, args.EventType);
            Assert.AreEqual(GiftPreference.Liked, args.GiftPreference);
            Assert.AreEqual(25, args.NewPoints);
        }

        #endregion
    }

    [TestFixture]
    public class DialogueEffectTests
    {
        #region DialogueEffect Tests

        [Test]
        public void DialogueEffect_DefaultPointsChange_IsZero()
        {
            var effect = new DialogueEffect();

            Assert.AreEqual(0, effect.PointsChange);
        }

        [Test]
        public void DialogueEffect_CanSetNpcId()
        {
            var effect = new DialogueEffect { NpcId = "npc_001" };

            Assert.AreEqual("npc_001", effect.NpcId);
        }

        [Test]
        public void DialogueEffect_CanSetTriggerEvent()
        {
            var effect = new DialogueEffect
            {
                NpcId = "npc_001",
                TriggerEventId = "event_after_dialogue"
            };

            Assert.AreEqual("event_after_dialogue", effect.TriggerEventId);
        }

        [Test]
        public void DialogueEffect_CanHavePositiveAndNegativePoints()
        {
            var positiveEffect = new DialogueEffect { PointsChange = 10 };
            var negativeEffect = new DialogueEffect { PointsChange = -5 };

            Assert.Greater(positiveEffect.PointsChange, 0);
            Assert.Less(negativeEffect.PointsChange, 0);
        }

        #endregion
    }

    [TestFixture]
    public class RelationshipEventTests
    {
        #region RelationshipEvent Tests

        [Test]
        public void RelationshipEvent_DefaultOneTime_IsFalse()
        {
            var evt = new RelationshipEvent();

            Assert.IsTrue(evt.OneTime);
        }

        [Test]
        public void RelationshipEvent_CanSetOneTime()
        {
            var evt = new RelationshipEvent { OneTime = true };

            Assert.IsTrue(evt.OneTime);
        }

        [Test]
        public void RelationshipEvent_CanSetRequiredLevel()
        {
            var evt = new RelationshipEvent
            {
                EventId = "confession",
                RequiredLevel = RelationshipLevel.Lover
            };

            Assert.AreEqual(RelationshipLevel.Lover, evt.RequiredLevel);
        }

        #endregion
    }
}
