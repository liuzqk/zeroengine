using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 生成条件基类
    /// </summary>
    public abstract class SpawnConditionBase : MonoBehaviour
    {
        [Header("Condition Settings")]
        [SerializeField] protected bool _invertResult = false;
        [SerializeField] protected bool _isEnabled = true;

        /// <summary>是否启用</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>是否反转结果</summary>
        public bool InvertResult
        {
            get => _invertResult;
            set => _invertResult = value;
        }

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        public bool IsMet()
        {
            if (!_isEnabled) return true; // 禁用时视为满足

            bool result = CheckCondition();
            return _invertResult ? !result : result;
        }

        /// <summary>
        /// 实际的条件检查逻辑 (子类实现)
        /// </summary>
        protected abstract bool CheckCondition();

        /// <summary>
        /// 重置条件状态
        /// </summary>
        public virtual void ResetCondition() { }

        /// <summary>
        /// 获取条件描述
        /// </summary>
        public virtual string GetDescription()
        {
            return GetType().Name;
        }

        /// <summary>
        /// 获取当前进度 (0-1)
        /// </summary>
        public virtual float GetProgress()
        {
            return IsMet() ? 1f : 0f;
        }
    }

    /// <summary>
    /// 复合条件 - 组合多个条件
    /// </summary>
    public class CompositeCondition : SpawnConditionBase
    {
        [Header("Composite Settings")]
        [SerializeField] private CompositeMode _mode = CompositeMode.All;
        [SerializeField] private SpawnConditionBase[] _conditions;

        public CompositeMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        protected override bool CheckCondition()
        {
            if (_conditions == null || _conditions.Length == 0)
            {
                return true;
            }

            switch (_mode)
            {
                case CompositeMode.All:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && !condition.IsMet())
                        {
                            return false;
                        }
                    }
                    return true;

                case CompositeMode.Any:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && condition.IsMet())
                        {
                            return true;
                        }
                    }
                    return false;

                case CompositeMode.None:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && condition.IsMet())
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    return true;
            }
        }

        public override void ResetCondition()
        {
            if (_conditions == null) return;

            foreach (var condition in _conditions)
            {
                condition?.ResetCondition();
            }
        }

        public override float GetProgress()
        {
            if (_conditions == null || _conditions.Length == 0)
            {
                return 1f;
            }

            float totalProgress = 0f;
            int count = 0;

            foreach (var condition in _conditions)
            {
                if (condition != null)
                {
                    totalProgress += condition.GetProgress();
                    count++;
                }
            }

            return count > 0 ? totalProgress / count : 1f;
        }
    }

    /// <summary>
    /// 复合模式
    /// </summary>
    public enum CompositeMode
    {
        /// <summary>所有条件都必须满足</summary>
        All,
        /// <summary>任一条件满足即可</summary>
        Any,
        /// <summary>所有条件都不满足</summary>
        None
    }
}
