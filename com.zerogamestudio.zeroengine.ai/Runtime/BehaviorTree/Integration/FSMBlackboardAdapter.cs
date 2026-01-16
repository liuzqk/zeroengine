using System;
using ZeroEngine.FSM;

namespace ZeroEngine.BehaviorTree.Integration
{
    /// <summary>
    /// FSM Blackboard 适配器，将 StateMachine 的黑板适配为 IBlackboard 接口
    /// 实现 BT 和 FSM 之间的数据共享
    /// </summary>
    public class FSMBlackboardAdapter : IBlackboard
    {
        private readonly StateMachine _machine;

        /// <inheritdoc/>
        public event Action<string, object> OnValueChanged;

        /// <summary>
        /// 创建 FSM 黑板适配器
        /// </summary>
        /// <param name="machine">要适配的状态机</param>
        public FSMBlackboardAdapter(StateMachine machine)
        {
            _machine = machine;
        }

        /// <inheritdoc/>
        public void SetValue<T>(string key, T value)
        {
            _machine.SetBlackboardValue(key, value);
            OnValueChanged?.Invoke(key, value);
        }

        /// <inheritdoc/>
        public T GetValue<T>(string key)
        {
            var value = _machine.GetBlackboardValue(key);
            if (value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            var rawValue = _machine.GetBlackboardValue(key);
            if (rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool HasKey(string key)
        {
            return _machine.GetBlackboardValue(key) != null;
        }

        /// <inheritdoc/>
        public bool RemoveKey(string key)
        {
            _machine.SetBlackboardValue(key, null);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            // FSM 黑板不直接支持清空，此处为兼容性实现
        }
    }
}
