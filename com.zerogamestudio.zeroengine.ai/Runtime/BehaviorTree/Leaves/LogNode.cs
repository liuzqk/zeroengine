using UnityEngine;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 日志节点：输出日志并返回成功（调试用）
    /// </summary>
    public class LogNode : BTLeaf
    {
        private readonly string _message;
        private readonly LogType _logType;

        /// <summary>
        /// 创建日志节点
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="logType">日志类型</param>
        public LogNode(string message, LogType logType = LogType.Log)
        {
            _message = message;
            _logType = logType;
            Name = $"Log({message})";
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            switch (_logType)
            {
                case LogType.Warning:
                    Debug.LogWarning($"[BT] {_message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[BT] {_message}");
                    break;
                default:
                    Debug.Log($"[BT] {_message}");
                    break;
            }
            return NodeState.Success;
        }
    }
}
