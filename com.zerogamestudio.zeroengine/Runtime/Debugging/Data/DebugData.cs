using System.Collections.Generic;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// BehaviorTree 节点调试数据
    /// </summary>
    public struct BTNodeDebugData
    {
        public string NodeName;
        public string NodeType;
        public BehaviorTree.NodeState State;
        public int ExecutionCount;
        public float LastExecutionTime;
        public int Depth;
        public bool IsActive;

        public override string ToString()
        {
            return $"{new string(' ', Depth * 2)}{NodeName} [{NodeType}]: {State}";
        }
    }

    /// <summary>
    /// FSM 状态转换记录
    /// </summary>
    public struct FSMTransitionRecord
    {
        public string FromState;
        public string ToState;
        public float Timestamp;
        public string Trigger;

        public override string ToString()
        {
            return $"[{Timestamp:F2}] {FromState} → {ToState}";
        }
    }

    /// <summary>
    /// Buff 调试数据
    /// </summary>
    public struct BuffDebugData
    {
        public string BuffId;
        public string BuffName;
        public int CurrentStacks;
        public int MaxStacks;
        public float RemainingTime;
        public float Duration;
        public bool IsExpired;
        public List<string> Modifiers;

        public float RemainingPercent => Duration > 0 ? RemainingTime / Duration : 0;

        public override string ToString()
        {
            string stackInfo = MaxStacks > 1 ? $" x{CurrentStacks}/{MaxStacks}" : "";
            string timeInfo = Duration > 0 ? $" ({RemainingTime:F1}s)" : "";
            return $"{BuffName}{stackInfo}{timeInfo}";
        }
    }

    /// <summary>
    /// Stat 调试数据
    /// </summary>
    public struct StatDebugData
    {
        public string StatName;
        public float BaseValue;
        public float CurrentValue;
        public float MinValue;
        public float MaxValue;
        public int ModifierCount;
        public List<StatModifierDebugData> Modifiers;

        public override string ToString()
        {
            string modInfo = ModifierCount > 0 ? $" (+{ModifierCount} mods)" : "";
            return $"{StatName}: {CurrentValue:F1} (base: {BaseValue:F1}){modInfo}";
        }
    }

    /// <summary>
    /// Stat 修饰器调试数据
    /// </summary>
    public struct StatModifierDebugData
    {
        public string Source;
        public string ModType;
        public float Value;

        public override string ToString()
        {
            string sign = Value >= 0 ? "+" : "";
            return $"  {sign}{Value:F1} ({ModType}) from {Source}";
        }
    }

    /// <summary>
    /// 对象池调试数据
    /// </summary>
    public struct PoolDebugData
    {
        public string PoolName;
        public int PooledCount;
        public int ActiveCount;
        public int TotalCreated;
        public int GetCount;
        public int ReturnCount;
        public float HitRate;

        public override string ToString()
        {
            return $"{PoolName}: {PooledCount} pooled, {ActiveCount} active, {HitRate:P0} hit rate";
        }
    }
}
