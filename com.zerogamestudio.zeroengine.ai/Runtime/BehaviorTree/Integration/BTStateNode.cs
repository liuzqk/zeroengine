using UnityEngine;
using ZeroEngine.FSM;

namespace ZeroEngine.BehaviorTree.Integration
{
    /// <summary>
    /// 在 FSM 状态中运行行为树的状态节点
    /// 继承此类并实现 ConfigureTree 方法来配置行为树
    /// </summary>
    public abstract class BTStateNode : IStateNode
    {
        /// <summary>所属状态机</summary>
        protected StateMachine Machine { get; private set; }

        /// <summary>行为树实例</summary>
        protected BehaviorTree Tree { get; private set; }

        /// <summary>共享黑板</summary>
        protected IBlackboard Blackboard { get; private set; }

        /// <inheritdoc/>
        public void OnCreate(StateMachine machine)
        {
            Machine = machine;

            // 创建适配器，使 BT 和 FSM 共享黑板数据
            Blackboard = new FSMBlackboardAdapter(machine);

            // 创建行为树
            Tree = new BehaviorTree(machine.Owner, Blackboard);

            // 子类配置行为树
            ConfigureTree(Tree);
        }

        /// <summary>
        /// 子类实现此方法配置行为树
        /// </summary>
        /// <param name="tree">要配置的行为树</param>
        protected abstract void ConfigureTree(BehaviorTree tree);

        /// <inheritdoc/>
        public virtual void OnEnter()
        {
            Tree.Start();
        }

        /// <inheritdoc/>
        public virtual void OnUpdate()
        {
            Tree.Tick(Time.deltaTime);

            // 检查树执行完毕后的状态转换
            if (Tree.CurrentState != NodeState.Running)
            {
                OnTreeCompleted(Tree.CurrentState);
            }
        }

        /// <inheritdoc/>
        public virtual void OnExit()
        {
            Tree.Stop();
        }

        /// <summary>
        /// 行为树执行完毕时调用，子类可重写以处理状态转换
        /// </summary>
        /// <param name="finalState">树的最终状态</param>
        protected virtual void OnTreeCompleted(NodeState finalState)
        {
            // 默认重启树，子类可以在这里切换 FSM 状态
            Tree.Restart();
        }
    }
}
