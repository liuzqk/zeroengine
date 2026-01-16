namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 行为树节点抽象基类
    /// </summary>
    public abstract class BTNode : IBTNode
    {
        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public virtual AbortMode AbortMode => AbortMode.None;

        /// <inheritdoc/>
        public NodeState CurrentState { get; protected set; } = NodeState.Failure;

        /// <summary>是否是首次执行</summary>
        protected bool _isFirstExecute = true;

        /// <inheritdoc/>
        public NodeState Execute(BTContext context)
        {
            if (_isFirstExecute)
            {
                OnStart(context);
                _isFirstExecute = false;
            }

            CurrentState = OnExecute(context);

            if (CurrentState != NodeState.Running)
            {
                OnStop(context);
                _isFirstExecute = true;
            }

            return CurrentState;
        }

        /// <inheritdoc/>
        public virtual void Abort()
        {
            if (CurrentState == NodeState.Running)
            {
                CurrentState = NodeState.Failure;
                OnAbort();
                _isFirstExecute = true;
            }
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            CurrentState = NodeState.Failure;
            _isFirstExecute = true;
        }

        /// <summary>节点开始执行时调用（首次进入）</summary>
        protected virtual void OnStart(BTContext context) { }

        /// <summary>节点执行逻辑（每帧调用直到返回非 Running）</summary>
        protected abstract NodeState OnExecute(BTContext context);

        /// <summary>节点停止时调用（成功或失败）</summary>
        protected virtual void OnStop(BTContext context) { }

        /// <summary>节点被中断时调用</summary>
        protected virtual void OnAbort() { }
    }
}
