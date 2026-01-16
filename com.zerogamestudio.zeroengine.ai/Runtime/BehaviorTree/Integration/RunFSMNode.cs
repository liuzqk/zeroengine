using System;
using ZeroEngine.FSM;

namespace ZeroEngine.BehaviorTree.Integration
{
    /// <summary>
    /// 在行为树中运行 FSM 的叶节点
    /// FSM 作为子系统在 BT 叶节点中执行
    /// </summary>
    public class RunFSMNode : BTLeaf
    {
        private StateMachine _machine;
        private readonly Action<StateMachine> _machineBuilder;
        private readonly Func<BTContext, StateMachine, bool> _completionCondition;
        private readonly bool _useSharedBlackboard;

        /// <summary>当前运行的状态机</summary>
        public StateMachine Machine => _machine;

        /// <summary>
        /// 创建 FSM 运行节点
        /// </summary>
        /// <param name="machineBuilder">FSM 构建器，用于配置状态机</param>
        /// <param name="completionCondition">完成条件，返回 true 时节点成功</param>
        /// <param name="useSharedBlackboard">是否与 BT 共享 Blackboard</param>
        public RunFSMNode(
            Action<StateMachine> machineBuilder,
            Func<BTContext, StateMachine, bool> completionCondition = null,
            bool useSharedBlackboard = true)
        {
            _machineBuilder = machineBuilder;
            _completionCondition = completionCondition;
            _useSharedBlackboard = useSharedBlackboard;
            Name = "RunFSM";
        }

        /// <inheritdoc/>
        protected override void OnStart(BTContext context)
        {
            // 创建新的 FSM 实例
            _machine = new StateMachine(context.Owner);

            // 如果启用共享黑板，同步 BT 黑板数据到 FSM
            if (_useSharedBlackboard && context.Blackboard is Blackboard btBlackboard)
            {
                // 通过适配器桥接数据
                // 注意：双向同步需要监听事件
            }

            // 配置状态机
            _machineBuilder?.Invoke(_machine);
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_machine == null) return NodeState.Failure;

            // 更新 FSM
            _machine.Update();

            // 检查完成条件
            if (_completionCondition != null && _completionCondition(context, _machine))
            {
                return NodeState.Success;
            }

            // FSM 持续运行
            return NodeState.Running;
        }

        /// <inheritdoc/>
        protected override void OnStop(BTContext context)
        {
            _machine = null;
        }

        /// <inheritdoc/>
        protected override void OnAbort()
        {
            _machine = null;
        }
    }

    /// <summary>
    /// RunFSMNode 的 Builder，提供流畅的 API
    /// </summary>
    public class RunFSMNodeBuilder
    {
        private Action<StateMachine> _machineBuilder;
        private Func<BTContext, StateMachine, bool> _completionCondition;
        private bool _useSharedBlackboard = true;
        private string _targetState;

        /// <summary>配置 FSM 状态</summary>
        public RunFSMNodeBuilder ConfigureMachine(Action<StateMachine> builder)
        {
            _machineBuilder = builder;
            return this;
        }

        /// <summary>当到达指定状态时视为完成</summary>
        public RunFSMNodeBuilder CompleteOnState<TState>() where TState : IStateNode
        {
            _targetState = typeof(TState).FullName;
            return this;
        }

        /// <summary>当到达指定状态时视为完成</summary>
        public RunFSMNodeBuilder CompleteOnState(string stateName)
        {
            _targetState = stateName;
            return this;
        }

        /// <summary>自定义完成条件</summary>
        public RunFSMNodeBuilder CompleteWhen(Func<BTContext, StateMachine, bool> condition)
        {
            _completionCondition = condition;
            return this;
        }

        /// <summary>设置是否共享黑板</summary>
        public RunFSMNodeBuilder ShareBlackboard(bool share)
        {
            _useSharedBlackboard = share;
            return this;
        }

        /// <summary>构建节点</summary>
        public RunFSMNode Build()
        {
            var finalCondition = _completionCondition;

            // 如果指定了目标状态，组合条件
            if (!string.IsNullOrEmpty(_targetState))
            {
                var targetState = _targetState;
                var originalCondition = _completionCondition;

                finalCondition = (ctx, machine) =>
                {
                    if (machine.CurrentNode == targetState)
                        return true;
                    return originalCondition?.Invoke(ctx, machine) ?? false;
                };
            }

            return new RunFSMNode(_machineBuilder, finalCondition, _useSharedBlackboard);
        }
    }
}
