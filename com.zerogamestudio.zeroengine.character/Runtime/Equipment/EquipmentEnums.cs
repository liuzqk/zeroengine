namespace ZeroEngine.Equipment
{
    /// <summary>
    /// 装备事件类型
    /// </summary>
    public enum EquipmentEventType
    {
        Equipped,       // 装备穿戴
        Unequipped,     // 装备卸下
        Enhanced,       // 装备强化
        Refined,        // 装备精炼
        Enchanted,      // 装备附魔
        GemSocketed,    // 宝石镶嵌
        GemRemoved,     // 宝石移除
        SetActivated,   // 套装激活
        SetDeactivated  // 套装失效
    }

    /// <summary>
    /// 强化结果
    /// </summary>
    public enum EnhanceResult
    {
        Success,        // 成功
        Failed,         // 失败
        MaxLevel,       // 已达最大等级
        InsufficientMaterial,  // 材料不足
        InsufficientCurrency   // 货币不足
    }

    /// <summary>
    /// 宝石槽位状态
    /// </summary>
    public enum GemSlotState
    {
        Locked,     // 未解锁
        Empty,      // 已解锁但为空
        Socketed    // 已镶嵌
    }
}
