using NUnit.Framework;
using ZeroEngine.FSM;

namespace ZeroEngine.Tests.FSM
{
    [TestFixture]
    public class StateMachineTests
    {
        private StateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine(null);
        }

        [TearDown]
        public void TearDown()
        {
            _machine = null;
        }

        #region Basic State Management

        [Test]
        public void AddNode_AddsState()
        {
            _machine.AddNode<TestStateA>();
            // No exception means success
            Assert.Pass();
        }

        [Test]
        public void Run_StartsWithInitialState()
        {
            _machine.AddNode<TestStateA>();
            _machine.Run<TestStateA>();

            Assert.AreEqual(typeof(TestStateA).FullName, _machine.CurrentNode);
        }

        [Test]
        public void ChangeState_SwitchesToNewState()
        {
            _machine.AddNode<TestStateA>();
            _machine.AddNode<TestStateB>();
            _machine.Run<TestStateA>();

            _machine.ChangeState<TestStateB>();

            Assert.AreEqual(typeof(TestStateB).FullName, _machine.CurrentNode);
        }

        [Test]
        public void ChangeState_TracksPreviousNode()
        {
            _machine.AddNode<TestStateA>();
            _machine.AddNode<TestStateB>();
            _machine.Run<TestStateA>();

            _machine.ChangeState<TestStateB>();

            Assert.AreEqual(typeof(TestStateA).FullName, _machine.PreviousNode);
        }

        [Test]
        public void ChangeState_CallsOnExitAndOnEnter()
        {
            var stateA = new TestStateA();
            var stateB = new TestStateB();

            _machine.AddNode(stateA);
            _machine.AddNode(stateB);
            _machine.Run<TestStateA>();

            Assert.IsTrue(stateA.OnEnterCalled);

            _machine.ChangeState<TestStateB>();

            Assert.IsTrue(stateA.OnExitCalled);
            Assert.IsTrue(stateB.OnEnterCalled);
        }

        #endregion

        #region Blackboard

        [Test]
        public void SetBlackboardValue_StoresValue()
        {
            _machine.SetBlackboardValue("health", 100);
            Assert.AreEqual(100, _machine.GetBlackboardValue("health"));
        }

        [Test]
        public void SetBlackboardValue_UpdatesExistingValue()
        {
            _machine.SetBlackboardValue("score", 10);
            _machine.SetBlackboardValue("score", 20);

            Assert.AreEqual(20, _machine.GetBlackboardValue("score"));
        }

        [Test]
        public void Blackboard_SharedBetweenStates()
        {
            _machine.AddNode<TestStateA>();
            _machine.AddNode<TestStateB>();
            _machine.Run<TestStateA>();

            _machine.SetBlackboardValue("shared", 42);
            _machine.ChangeState<TestStateB>();

            Assert.AreEqual(42, _machine.GetBlackboardValue("shared"));
        }

        #endregion

        #region State Lifecycle

        [Test]
        public void Update_CalledOnCurrentState()
        {
            var state = new TestStateA();
            _machine.AddNode(state);
            _machine.Run<TestStateA>();

            _machine.Update();

            Assert.IsTrue(state.OnUpdateCalled);
        }

        [Test]
        public void AddNode_Instance_CallsOnCreate()
        {
            var state = new TestStateA();
            _machine.AddNode(state);

            Assert.IsTrue(state.OnCreateCalled);
        }

        #endregion

        #region Test State Classes

        private class TestStateA : IStateNode
        {
            public bool OnCreateCalled { get; private set; }
            public bool OnEnterCalled { get; private set; }
            public bool OnExitCalled { get; private set; }
            public bool OnUpdateCalled { get; private set; }

            public void OnCreate(StateMachine machine) => OnCreateCalled = true;
            public void OnEnter() => OnEnterCalled = true;
            public void OnUpdate() => OnUpdateCalled = true;
            public void OnExit() => OnExitCalled = true;
        }

        private class TestStateB : IStateNode
        {
            public bool OnEnterCalled { get; private set; }
            public bool OnExitCalled { get; private set; }

            public void OnCreate(StateMachine machine) { }
            public void OnEnter() => OnEnterCalled = true;
            public void OnUpdate() { }
            public void OnExit() => OnExitCalled = true;
        }

        #endregion
    }
}
