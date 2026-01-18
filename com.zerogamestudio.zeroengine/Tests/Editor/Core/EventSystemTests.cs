using System;
using NUnit.Framework;
using ZeroEngine.Core;

namespace ZeroEngine.Tests.Core
{
    /// <summary>
    /// EventManager 单元测试
    /// </summary>
    [TestFixture]
    public class EventSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            EventManager.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventManager.Clear();
        }

        #region No Arguments Tests

        [Test]
        public void Subscribe_WithValidListener_CanBeTriggered()
        {
            // Arrange
            bool triggered = false;
            Action listener = () => triggered = true;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent");

            // Assert
            Assert.IsTrue(triggered);
        }

        [Test]
        public void Trigger_WithNoSubscribers_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => EventManager.Trigger("NonExistentEvent"));
        }

        [Test]
        public void Unsubscribe_RemovesListener()
        {
            // Arrange
            int count = 0;
            Action listener = () => count++;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent");
            EventManager.Unsubscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent");

            // Assert
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Subscribe_MultipleListeners_AllTriggered()
        {
            // Arrange
            int count = 0;
            Action listener1 = () => count++;
            Action listener2 = () => count++;
            Action listener3 = () => count++;

            // Act
            EventManager.Subscribe("TestEvent", listener1);
            EventManager.Subscribe("TestEvent", listener2);
            EventManager.Subscribe("TestEvent", listener3);
            EventManager.Trigger("TestEvent");

            // Assert
            Assert.AreEqual(3, count);
        }

        [Test]
        public void Unsubscribe_OnlyRemovesSpecificListener()
        {
            // Arrange
            int count1 = 0, count2 = 0;
            Action listener1 = () => count1++;
            Action listener2 = () => count2++;

            // Act
            EventManager.Subscribe("TestEvent", listener1);
            EventManager.Subscribe("TestEvent", listener2);
            EventManager.Unsubscribe("TestEvent", listener1);
            EventManager.Trigger("TestEvent");

            // Assert
            Assert.AreEqual(0, count1);
            Assert.AreEqual(1, count2);
        }

        #endregion

        #region Single Argument Tests

        [Test]
        public void SubscribeGeneric_WithOneArg_ReceivesArgument()
        {
            // Arrange
            int receivedValue = 0;
            Action<int> listener = (value) => receivedValue = value;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 42);

            // Assert
            Assert.AreEqual(42, receivedValue);
        }

        [Test]
        public void UnsubscribeGeneric_WithOneArg_RemovesListener()
        {
            // Arrange
            int count = 0;
            Action<int> listener = (_) => count++;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 1);
            EventManager.Unsubscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 2);

            // Assert
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TriggerGeneric_WithStringArg_PassesCorrectValue()
        {
            // Arrange
            string receivedValue = null;
            Action<string> listener = (value) => receivedValue = value;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", "Hello World");

            // Assert
            Assert.AreEqual("Hello World", receivedValue);
        }

        #endregion

        #region Two Arguments Tests

        [Test]
        public void SubscribeGeneric_WithTwoArgs_ReceivesBothArguments()
        {
            // Arrange
            int receivedInt = 0;
            string receivedString = null;
            Action<int, string> listener = (i, s) =>
            {
                receivedInt = i;
                receivedString = s;
            };

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 100, "test");

            // Assert
            Assert.AreEqual(100, receivedInt);
            Assert.AreEqual("test", receivedString);
        }

        [Test]
        public void UnsubscribeGeneric_WithTwoArgs_RemovesListener()
        {
            // Arrange
            int count = 0;
            Action<int, string> listener = (_, __) => count++;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 1, "a");
            EventManager.Unsubscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent", 2, "b");

            // Assert
            Assert.AreEqual(1, count);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_RemovesAllListeners()
        {
            // Arrange
            int count = 0;
            EventManager.Subscribe("Event1", () => count++);
            EventManager.Subscribe("Event2", (int _) => count++);
            EventManager.Subscribe("Event3", (int _, string __) => count++);

            // Act
            EventManager.Clear();
            EventManager.Trigger("Event1");
            EventManager.Trigger("Event2", 1);
            EventManager.Trigger("Event3", 1, "test");

            // Assert
            Assert.AreEqual(0, count);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Subscribe_SameListenerTwice_TriggeredTwice()
        {
            // Arrange
            int count = 0;
            Action listener = () => count++;

            // Act
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Subscribe("TestEvent", listener);
            EventManager.Trigger("TestEvent");

            // Assert
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Unsubscribe_NonExistentEvent_DoesNotThrow()
        {
            // Arrange
            Action listener = () => { };

            // Act & Assert
            Assert.DoesNotThrow(() => EventManager.Unsubscribe("NonExistent", listener));
        }

        [Test]
        public void Unsubscribe_NonExistentListener_DoesNotThrow()
        {
            // Arrange
            Action listener1 = () => { };
            Action listener2 = () => { };
            EventManager.Subscribe("TestEvent", listener1);

            // Act & Assert
            Assert.DoesNotThrow(() => EventManager.Unsubscribe("TestEvent", listener2));
        }

        #endregion

        #region GameEvents Constants Tests

        [Test]
        public void GameEvents_ConstantsAreNotEmpty()
        {
            // Assert - 验证常量定义正确
            Assert.IsFalse(string.IsNullOrEmpty(GameEvents.GameStarted));
            Assert.IsFalse(string.IsNullOrEmpty(GameEvents.BattleStart));
            Assert.IsFalse(string.IsNullOrEmpty(GameEvents.ItemObtained));
            Assert.IsFalse(string.IsNullOrEmpty(GameEvents.QuestCompleted));
        }

        [Test]
        public void GameEvents_CanBeUsedWithEventManager()
        {
            // Arrange
            bool triggered = false;

            // Act
            EventManager.Subscribe(GameEvents.GameStarted, () => triggered = true);
            EventManager.Trigger(GameEvents.GameStarted);

            // Assert
            Assert.IsTrue(triggered);
        }

        #endregion
    }
}
