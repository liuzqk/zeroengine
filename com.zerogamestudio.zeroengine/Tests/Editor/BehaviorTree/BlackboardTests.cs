using NUnit.Framework;
using ZeroEngine.BehaviorTree;
using UnityEngine;

namespace ZeroEngine.Tests.BehaviorTree
{
    [TestFixture]
    public class BlackboardTests
    {
        private Blackboard _blackboard;

        [SetUp]
        public void SetUp()
        {
            _blackboard = new Blackboard();
        }

        #region Generic SetValue/GetValue

        [Test]
        public void SetValue_Generic_StoresValue()
        {
            _blackboard.SetValue("test", "hello");
            Assert.AreEqual("hello", _blackboard.GetValue<string>("test"));
        }

        [Test]
        public void GetValue_NonExistentKey_ReturnsDefault()
        {
            Assert.AreEqual(default(int), _blackboard.GetValue<int>("nonexistent"));
            Assert.IsNull(_blackboard.GetValue<string>("nonexistent"));
        }

        [Test]
        public void TryGetValue_ExistingKey_ReturnsTrue()
        {
            _blackboard.SetValue("test", 42);
            Assert.IsTrue(_blackboard.TryGetValue<int>("test", out var value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void TryGetValue_NonExistentKey_ReturnsFalse()
        {
            Assert.IsFalse(_blackboard.TryGetValue<int>("nonexistent", out _));
        }

        #endregion

        #region Typed Accessors (Zero Boxing)

        [Test]
        public void SetInt_GetInt_ZeroBoxing()
        {
            _blackboard.SetInt("score", 100);
            Assert.AreEqual(100, _blackboard.GetInt("score"));
            Assert.AreEqual(0, _blackboard.GetInt("nonexistent"));
            Assert.AreEqual(-1, _blackboard.GetInt("nonexistent", -1));
        }

        [Test]
        public void SetFloat_GetFloat_ZeroBoxing()
        {
            _blackboard.SetFloat("speed", 5.5f);
            TestHelpers.AssertApproximatelyEqual(5.5f, _blackboard.GetFloat("speed"));
            TestHelpers.AssertApproximatelyEqual(0f, _blackboard.GetFloat("nonexistent"));
            TestHelpers.AssertApproximatelyEqual(1.5f, _blackboard.GetFloat("nonexistent", 1.5f));
        }

        [Test]
        public void SetBool_GetBool_ZeroBoxing()
        {
            _blackboard.SetBool("isActive", true);
            Assert.IsTrue(_blackboard.GetBool("isActive"));
            Assert.IsFalse(_blackboard.GetBool("nonexistent"));
            Assert.IsTrue(_blackboard.GetBool("nonexistent", true));
        }

        [Test]
        public void SetVector3_GetVector3_ZeroBoxing()
        {
            var position = new Vector3(1, 2, 3);
            _blackboard.SetVector3("position", position);
            Assert.AreEqual(position, _blackboard.GetVector3("position"));
            Assert.AreEqual(Vector3.zero, _blackboard.GetVector3("nonexistent"));
        }

        [Test]
        public void SetVector2_GetVector2_ZeroBoxing()
        {
            var velocity = new Vector2(4, 5);
            _blackboard.SetVector2("velocity", velocity);
            Assert.AreEqual(velocity, _blackboard.GetVector2("velocity"));
            Assert.AreEqual(Vector2.zero, _blackboard.GetVector2("nonexistent"));
        }

        #endregion

        #region Key Operations

        [Test]
        public void HasKey_ReturnsCorrectly()
        {
            Assert.IsFalse(_blackboard.HasKey("test"));
            _blackboard.SetValue("test", 1);
            Assert.IsTrue(_blackboard.HasKey("test"));
        }

        [Test]
        public void RemoveKey_RemovesValue()
        {
            _blackboard.SetValue("test", 42);
            Assert.IsTrue(_blackboard.RemoveKey("test"));
            Assert.IsFalse(_blackboard.HasKey("test"));
            Assert.IsFalse(_blackboard.RemoveKey("test")); // Already removed
        }

        [Test]
        public void Clear_RemovesAllValues()
        {
            _blackboard.SetValue("a", 1);
            _blackboard.SetInt("b", 2);
            _blackboard.SetFloat("c", 3f);
            _blackboard.Clear();

            Assert.IsFalse(_blackboard.HasKey("a"));
            Assert.AreEqual(0, _blackboard.GetInt("b"));
            TestHelpers.AssertApproximatelyEqual(0f, _blackboard.GetFloat("c"));
        }

        #endregion

        #region Events

        [Test]
        public void OnValueChanged_FiresOnSet()
        {
            var counter = TestHelpers.CreateEventCounter();
            string changedKey = null;
            object changedValue = null;

            _blackboard.OnValueChanged += (key, value) =>
            {
                counter.Increment();
                changedKey = key;
                changedValue = value;
            };

            _blackboard.SetValue("test", 42);

            counter.AssertCount(1);
            Assert.AreEqual("test", changedKey);
            Assert.AreEqual(42, changedValue);
        }

        #endregion
    }
}
