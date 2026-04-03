using UnityEngine;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 队伍配置 - 定义队伍结构
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "ZeroEngine/Party/Party Config")]
    public class PartyConfigSO : ScriptableObject
    {
        [Header("出战队伍")]
        [Tooltip("出战队伍最大人数")]
        [Range(1, 12)]
        public int MaxActiveMembers = 4;

        [Header("后备队伍")]
        [Tooltip("后备队伍最大人数")]
        [Range(0, 20)]
        public int MaxReserveMembers = 4;

        [Header("其他槽位")]
        [Tooltip("临时槽位数量 (战斗召唤等)")]
        [Range(0, 4)]
        public int MaxTemporarySlots = 2;

        [Tooltip("宠物槽位数量")]
        [Range(0, 4)]
        public int MaxPetSlots = 1;

        [Header("规则")]
        [Tooltip("是否允许战斗中切换")]
        public bool AllowSwitchInCombat = true;

        [Tooltip("战斗中切换消耗行动")]
        public bool SwitchCostsAction = true;

        [Tooltip("是否允许重复成员 (分身等)")]
        public bool AllowDuplicateMembers = false;

        /// <summary>
        /// 总槽位数
        /// </summary>
        public int TotalSlots => MaxActiveMembers + MaxReserveMembers + MaxTemporarySlots + MaxPetSlots;
    }
}
