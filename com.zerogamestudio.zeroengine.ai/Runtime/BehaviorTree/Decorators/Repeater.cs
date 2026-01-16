namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 重复节点：重复执行子节点指定次数或无限次
    /// </summary>
    public class Repeater : BTDecorator
    {
        private readonly int _repeatCount;
        private readonly bool _infinite;
        private int _currentCount;

        /// <summary>
        /// 创建重复节点
        /// </summary>
        /// <param name="count">重复次数，-1 表示无限重复</param>
        public Repeater(int count = -1)
        {
            _repeatCount = count;
            _infinite = count < 0;
            Name = _infinite ? "Repeater(∞)" : $"Repeater({count})";
        }

        /// <inheritdoc/>
        protected override void OnStart(BTContext context)
        {
            _currentCount = 0;
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_child == null) return NodeState.Failure;

            var state = _child.Execute(context);

            if (state == NodeState.Running)
            {
                return NodeState.Running;
            }

            _currentCount++;

            if (!_infinite && _currentCount >= _repeatCount)
            {
                return NodeState.Success;
            }

            // 重置子节点以便下次执行
            _child.Reset();
            return NodeState.Running;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _currentCount = 0;
        }
    }
}
