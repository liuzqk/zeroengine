namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 等待节点：等待指定时间后成功
    /// </summary>
    public class WaitNode : BTLeaf
    {
        private readonly float _duration;
        private float _elapsedTime;

        /// <summary>
        /// 创建等待节点
        /// </summary>
        /// <param name="duration">等待时间（秒）</param>
        public WaitNode(float duration)
        {
            _duration = duration;
            Name = $"Wait({duration}s)";
        }

        /// <inheritdoc/>
        protected override void OnStart(BTContext context)
        {
            _elapsedTime = 0f;
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            _elapsedTime += context.DeltaTime;

            if (_elapsedTime >= _duration)
            {
                return NodeState.Success;
            }

            return NodeState.Running;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _elapsedTime = 0f;
        }
    }
}
