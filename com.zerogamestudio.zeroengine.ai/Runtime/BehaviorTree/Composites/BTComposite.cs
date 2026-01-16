using System.Collections.Generic;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 组合节点抽象基类，包含多个子节点
    /// </summary>
    public abstract class BTComposite : BTNode
    {
        /// <summary>子节点列表</summary>
        protected readonly List<IBTNode> _children = new List<IBTNode>();

        /// <summary>当前执行的子节点索引</summary>
        protected int _currentChildIndex = 0;

        /// <summary>子节点只读列表</summary>
        public IReadOnlyList<IBTNode> Children => _children;

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="child">子节点</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BTComposite AddChild(IBTNode child)
        {
            _children.Add(child);
            return this;
        }

        /// <summary>
        /// 添加多个子节点
        /// </summary>
        /// <param name="children">子节点数组</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BTComposite AddChildren(params IBTNode[] children)
        {
            _children.AddRange(children);
            return this;
        }

        /// <inheritdoc/>
        public override void Abort()
        {
            // 中断当前正在执行的子节点
            if (_currentChildIndex < _children.Count)
            {
                _children[_currentChildIndex].Abort();
            }
            base.Abort();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _currentChildIndex = 0;
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Reset();
            }
        }

        /// <inheritdoc/>
        protected override void OnStart(BTContext context)
        {
            _currentChildIndex = 0;
        }

        /// <summary>
        /// 检查是否有条件中断触发
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <param name="fromIndex">从哪个索引开始检查</param>
        /// <returns>是否触发了中断</returns>
        protected bool CheckAbortConditions(BTContext context, int fromIndex)
        {
            for (int i = 0; i < fromIndex; i++)
            {
                var child = _children[i];
                if ((child.AbortMode & AbortMode.LowerPriority) != 0)
                {
                    // 重新评估条件节点
                    if (child is BTDecorator decorator && decorator.CheckCondition(context))
                    {
                        // 中断当前节点并从此处重新开始
                        AbortFromIndex(_currentChildIndex);
                        _currentChildIndex = i;
                        return true;
                    }
                }
            }
            return false;
        }

        private void AbortFromIndex(int index)
        {
            if (index < _children.Count)
            {
                _children[index].Abort();
            }
        }
    }
}
