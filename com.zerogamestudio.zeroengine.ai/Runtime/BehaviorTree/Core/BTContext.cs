namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 行为树执行上下文，在节点间传递共享数据
    /// </summary>
    public class BTContext
    {
        /// <summary>行为树拥有者（通常是 MonoBehaviour 或 GameObject）</summary>
        public object Owner { get; }

        /// <summary>共享黑板数据</summary>
        public IBlackboard Blackboard { get; }

        /// <summary>当前帧的 DeltaTime</summary>
        public float DeltaTime { get; set; }

        /// <summary>行为树控制器引用</summary>
        public BehaviorTree Tree { get; }

        /// <summary>
        /// 创建行为树执行上下文
        /// </summary>
        /// <param name="owner">行为树拥有者</param>
        /// <param name="blackboard">共享黑板</param>
        /// <param name="tree">行为树控制器</param>
        public BTContext(object owner, IBlackboard blackboard, BehaviorTree tree)
        {
            Owner = owner;
            Blackboard = blackboard;
            Tree = tree;
        }
    }
}
