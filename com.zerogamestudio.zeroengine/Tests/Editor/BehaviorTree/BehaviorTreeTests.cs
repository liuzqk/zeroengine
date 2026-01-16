using NUnit.Framework;
using ZeroEngine.BehaviorTree;

namespace ZeroEngine.Tests.BehaviorTree
{
    [TestFixture]
    public class BehaviorTreeTests
    {
        #region ActionNode Tests

        [Test]
        public void ActionNode_ReturnsSuccess()
        {
            var node = new ActionNode(ctx => NodeState.Success);
            var context = CreateContext();

            Assert.AreEqual(NodeState.Success, node.Execute(context));
        }

        [Test]
        public void ActionNode_ReturnsFailure()
        {
            var node = new ActionNode(ctx => NodeState.Failure);
            var context = CreateContext();

            Assert.AreEqual(NodeState.Failure, node.Execute(context));
        }

        [Test]
        public void ActionNode_ReturnsRunning()
        {
            var node = new ActionNode(ctx => NodeState.Running);
            var context = CreateContext();

            Assert.AreEqual(NodeState.Running, node.Execute(context));
        }

        #endregion

        #region Sequence Tests

        [Test]
        public void Sequence_AllSuccess_ReturnsSuccess()
        {
            var sequence = new Sequence()
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, sequence.Execute(context));
        }

        [Test]
        public void Sequence_OneFailure_ReturnsFailure()
        {
            var sequence = new Sequence()
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Failure))
                .AddChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, sequence.Execute(context));
        }

        [Test]
        public void Sequence_OneRunning_ReturnsRunning()
        {
            int callCount = 0;
            var sequence = new Sequence()
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Running))
                .AddChild(new ActionNode(ctx => { callCount++; return NodeState.Success; }));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Running, sequence.Execute(context));
            Assert.AreEqual(0, callCount); // Third node should not be called
        }

        #endregion

        #region Selector Tests

        [Test]
        public void Selector_FirstSuccess_ReturnsSuccess()
        {
            int callCount = 0;
            var selector = new Selector()
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => { callCount++; return NodeState.Success; }));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, selector.Execute(context));
            Assert.AreEqual(0, callCount); // Second node should not be called
        }

        [Test]
        public void Selector_AllFailure_ReturnsFailure()
        {
            var selector = new Selector()
                .AddChild(new ActionNode(ctx => NodeState.Failure))
                .AddChild(new ActionNode(ctx => NodeState.Failure))
                .AddChild(new ActionNode(ctx => NodeState.Failure));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, selector.Execute(context));
        }

        [Test]
        public void Selector_SkipsFailureFindSuccess()
        {
            var selector = new Selector()
                .AddChild(new ActionNode(ctx => NodeState.Failure))
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Failure));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, selector.Execute(context));
        }

        #endregion

        #region Parallel Tests

        [Test]
        public void Parallel_RequireAllSuccess_AllSuccess_ReturnsSuccess()
        {
            var parallel = new Parallel(ParallelPolicy.RequireAll, ParallelPolicy.RequireOne)
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, parallel.Execute(context));
        }

        [Test]
        public void Parallel_RequireOneSuccess_OneSuccess_ReturnsSuccess()
        {
            var parallel = new Parallel(ParallelPolicy.RequireOne, ParallelPolicy.RequireAll)
                .AddChild(new ActionNode(ctx => NodeState.Running))
                .AddChild(new ActionNode(ctx => NodeState.Success))
                .AddChild(new ActionNode(ctx => NodeState.Running));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, parallel.Execute(context));
        }

        [Test]
        public void Parallel_RequireOneFailure_OneFailure_ReturnsFailure()
        {
            var parallel = new Parallel(ParallelPolicy.RequireAll, ParallelPolicy.RequireOne)
                .AddChild(new ActionNode(ctx => NodeState.Running))
                .AddChild(new ActionNode(ctx => NodeState.Failure))
                .AddChild(new ActionNode(ctx => NodeState.Running));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, parallel.Execute(context));
        }

        #endregion

        #region Decorator Tests

        [Test]
        public void Inverter_InvertsSuccess()
        {
            var inverter = new Inverter();
            inverter.SetChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, inverter.Execute(context));
        }

        [Test]
        public void Inverter_InvertsFailure()
        {
            var inverter = new Inverter();
            inverter.SetChild(new ActionNode(ctx => NodeState.Failure));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, inverter.Execute(context));
        }

        [Test]
        public void AlwaysSucceed_ReturnsSuccess()
        {
            var decorator = new AlwaysSucceed();
            decorator.SetChild(new ActionNode(ctx => NodeState.Failure));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, decorator.Execute(context));
        }

        [Test]
        public void AlwaysFail_ReturnsFailure()
        {
            var decorator = new AlwaysFail();
            decorator.SetChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, decorator.Execute(context));
        }

        [Test]
        public void Repeater_RepeatsSpecifiedTimes()
        {
            int count = 0;
            var repeater = new Repeater(3);
            repeater.SetChild(new ActionNode(ctx => { count++; return NodeState.Success; }));

            var context = CreateContext();

            // Execute until complete
            NodeState state;
            do
            {
                state = repeater.Execute(context);
            } while (state == NodeState.Running);

            Assert.AreEqual(3, count);
            Assert.AreEqual(NodeState.Success, state);
        }

        [Test]
        public void Conditional_TrueCondition_ExecutesChild()
        {
            var conditional = new Conditional(ctx => true);
            conditional.SetChild(new ActionNode(ctx => NodeState.Success));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Success, conditional.Execute(context));
        }

        [Test]
        public void Conditional_FalseCondition_ReturnsFailure()
        {
            int childCalled = 0;
            var conditional = new Conditional(ctx => false);
            conditional.SetChild(new ActionNode(ctx => { childCalled++; return NodeState.Success; }));

            var context = CreateContext();
            Assert.AreEqual(NodeState.Failure, conditional.Execute(context));
            Assert.AreEqual(0, childCalled);
        }

        #endregion

        #region Blackboard Integration

        [Test]
        public void Blackboard_SharedBetweenNodes()
        {
            var blackboard = new Blackboard();
            var context = new BTContext(null, blackboard, null);

            var sequence = new Sequence()
                .AddChild(new ActionNode(ctx =>
                {
                    ctx.Blackboard.SetValue("counter", 1);
                    return NodeState.Success;
                }))
                .AddChild(new ActionNode(ctx =>
                {
                    var counter = ctx.Blackboard.GetValue<int>("counter");
                    ctx.Blackboard.SetValue("counter", counter + 1);
                    return NodeState.Success;
                }));

            sequence.Execute(context);

            Assert.AreEqual(2, blackboard.GetValue<int>("counter"));
        }

        #endregion

        private BTContext CreateContext()
        {
            return new BTContext(null, new Blackboard(), null);
        }
    }
}
