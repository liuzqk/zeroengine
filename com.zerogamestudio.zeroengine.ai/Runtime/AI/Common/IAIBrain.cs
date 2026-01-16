using UnityEngine;

namespace ZeroEngine.AI
{
    /// <summary>
    /// AI 大脑通用接口
    /// 所有 AI 决策系统 (UtilityAI, GOAP, NPCSchedule) 都实现此接口
    /// </summary>
    public interface IAIBrain
    {
        /// <summary>是否激活</summary>
        bool IsActive { get; set; }

        /// <summary>当前执行的行动名称</summary>
        string CurrentActionName { get; }

        /// <summary>
        /// 初始化 AI 大脑
        /// </summary>
        void Initialize(AIContext context);

        /// <summary>
        /// 每帧更新 (由 AIAgent 调用)
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 强制重新评估决策
        /// </summary>
        void ForceReevaluate();

        /// <summary>
        /// 停止当前行动
        /// </summary>
        void StopCurrentAction();

        /// <summary>
        /// 重置 AI 状态
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// AI 大脑优先级 (用于多大脑协作)
    /// </summary>
    public enum AIBrainPriority
    {
        /// <summary>低优先级 (背景行为)</summary>
        Low = 0,
        /// <summary>正常优先级</summary>
        Normal = 50,
        /// <summary>高优先级 (紧急行为)</summary>
        High = 100,
        /// <summary>最高优先级 (覆盖一切)</summary>
        Critical = 200
    }

    /// <summary>
    /// AI 行动状态
    /// </summary>
    public enum AIActionState
    {
        /// <summary>空闲</summary>
        Idle,
        /// <summary>执行中</summary>
        Running,
        /// <summary>成功完成</summary>
        Success,
        /// <summary>失败</summary>
        Failed,
        /// <summary>被取消</summary>
        Cancelled
    }

    /// <summary>
    /// AI 行动结果
    /// </summary>
    public readonly struct AIActionResult
    {
        public readonly AIActionState State;
        public readonly string Message;

        public AIActionResult(AIActionState state, string message = null)
        {
            State = state;
            Message = message;
        }

        public static AIActionResult Running() => new(AIActionState.Running);
        public static AIActionResult Success() => new(AIActionState.Success);
        public static AIActionResult Failed(string reason = null) => new(AIActionState.Failed, reason);
        public static AIActionResult Cancelled() => new(AIActionState.Cancelled);

        public bool IsComplete => State == AIActionState.Success ||
                                  State == AIActionState.Failed ||
                                  State == AIActionState.Cancelled;
    }
}
