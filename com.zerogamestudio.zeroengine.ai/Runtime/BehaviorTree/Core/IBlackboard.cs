using System;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 黑板数据共享接口，用于 BT 和 FSM 之间的数据通信
    /// </summary>
    public interface IBlackboard
    {
        /// <summary>设置黑板值</summary>
        void SetValue<T>(string key, T value);

        /// <summary>获取黑板值</summary>
        T GetValue<T>(string key);

        /// <summary>尝试获取黑板值</summary>
        bool TryGetValue<T>(string key, out T value);

        /// <summary>检查键是否存在</summary>
        bool HasKey(string key);

        /// <summary>移除键</summary>
        bool RemoveKey(string key);

        /// <summary>清空所有数据</summary>
        void Clear();

        /// <summary>值变更事件</summary>
        event Action<string, object> OnValueChanged;
    }
}
