using UnityEngine;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 行为树控制器，管理树的执行和生命周期
    /// </summary>
    public class BehaviorTree
    {
        private IBTNode _root;
        private readonly BTContext _context;
        private bool _isRunning;

        /// <summary>行为树拥有者</summary>
        public object Owner { get; }

        /// <summary>共享黑板</summary>
        public IBlackboard Blackboard { get; }

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>当前树状态</summary>
        public NodeState CurrentState => _root?.CurrentState ?? NodeState.Failure;

        /// <summary>根节点</summary>
        public IBTNode Root => _root;

        /// <summary>
        /// 创建行为树
        /// </summary>
        /// <param name="owner">拥有者（通常是 MonoBehaviour）</param>
        /// <param name="blackboard">共享黑板，不指定则自动创建</param>
        public BehaviorTree(object owner, IBlackboard blackboard = null)
        {
            Owner = owner;
            Blackboard = blackboard ?? new Blackboard();
            _context = new BTContext(owner, Blackboard, this);
        }

        /// <summary>
        /// 设置根节点
        /// </summary>
        /// <param name="root">根节点</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BehaviorTree SetRoot(IBTNode root)
        {
            _root = root;
            return this;
        }

        /// <summary>
        /// 开始执行行为树
        /// </summary>
        public void Start()
        {
            if (_root == null)
            {
                Debug.LogWarning("[ZeroEngine.BehaviorTree] No root node set.");
                return;
            }

            _isRunning = true;
            _root.Reset();
        }

        /// <summary>
        /// 停止执行行为树
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _root?.Abort();
                _isRunning = false;
            }
        }

        /// <summary>
        /// 每帧更新（在 MonoBehaviour.Update 中调用）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _root == null) return;

            _context.DeltaTime = deltaTime;
            _root.Execute(_context);
        }

        /// <summary>
        /// 使用 Time.deltaTime 更新
        /// </summary>
        public void Tick()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// 重置并重新开始执行
        /// </summary>
        public void Restart()
        {
            _root?.Reset();
            _isRunning = true;
        }
    }
}
