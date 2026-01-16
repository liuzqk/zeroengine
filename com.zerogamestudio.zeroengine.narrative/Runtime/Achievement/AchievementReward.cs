using System;
using UnityEngine;

namespace ZeroEngine.Achievement
{
    /// <summary>
    /// 成就奖励基类
    /// </summary>
    [Serializable]
    public abstract class AchievementReward
    {
        /// <summary>奖励描述</summary>
        public abstract string Description { get; }

        /// <summary>发放奖励</summary>
        public abstract void Grant();

        /// <summary>获取奖励图标（可选）</summary>
        public virtual Sprite GetIcon() => null;
    }

    /// <summary>
    /// 物品奖励
    /// </summary>
    [Serializable]
    public class ItemReward : AchievementReward
    {
        [Tooltip("奖励物品")]
        public Inventory.InventoryItemSO Item;

        [Tooltip("数量")]
        public int Amount = 1;

        public override string Description =>
            Item != null ? $"{Item.ItemName} x{Amount}" : "物品奖励";

        public override void Grant()
        {
            if (Item == null || Amount <= 0) return;

            var inventory = Inventory.InventoryManager.Instance;
            if (inventory != null)
            {
                inventory.AddItem(Item, Amount);
            }
        }

        public override Sprite GetIcon() => Item?.Icon;
    }

    /// <summary>
    /// 货币奖励
    /// </summary>
    [Serializable]
    public class CurrencyReward : AchievementReward
    {
        [Tooltip("货币类型")]
        public Loot.CurrencyType CurrencyType;

        [Tooltip("数量")]
        public int Amount = 100;

        public override string Description =>
            $"{CurrencyType} x{Amount}";

        public override void Grant()
        {
            if (Amount <= 0) return;

            // 需要外部货币系统支持
            // CurrencyManager.Instance?.AddCurrency(CurrencyType, Amount);
        }
    }

    /// <summary>
    /// 经验奖励
    /// </summary>
    [Serializable]
    public class ExpReward : AchievementReward
    {
        [Tooltip("经验类型")]
        public ExpType ExpType = ExpType.Character;

        [Tooltip("经验值")]
        public int Amount = 100;

        public override string Description =>
            $"{ExpType}经验 +{Amount}";

        public override void Grant()
        {
            if (Amount <= 0) return;

            // 需要外部经验系统支持
            // ExpManager.Instance?.AddExp(ExpType, Amount);
        }
    }

    /// <summary>
    /// 经验类型
    /// </summary>
    public enum ExpType
    {
        Character,
        Skill,
        Profession
    }

    /// <summary>
    /// 解锁奖励（解锁内容/功能）
    /// </summary>
    [Serializable]
    public class UnlockReward : AchievementReward
    {
        [Tooltip("解锁类型")]
        public UnlockType Type = UnlockType.Feature;

        [Tooltip("解锁ID")]
        public string UnlockId;

        [Tooltip("解锁描述")]
        public string UnlockDescription;

        public override string Description =>
            !string.IsNullOrEmpty(UnlockDescription) ? UnlockDescription : $"解锁: {UnlockId}";

        public override void Grant()
        {
            if (string.IsNullOrEmpty(UnlockId)) return;

            // 通知解锁系统
            _unlockCallbacks.TryGetValue(UnlockId, out var callback);
            callback?.Invoke();
        }

        // 解锁回调注册
        private static readonly System.Collections.Generic.Dictionary<string, Action> _unlockCallbacks =
            new System.Collections.Generic.Dictionary<string, Action>();

        public static void RegisterUnlockCallback(string id, Action callback)
        {
            _unlockCallbacks[id] = callback;
        }

        public static void UnregisterUnlockCallback(string id)
        {
            _unlockCallbacks.Remove(id);
        }
    }

    /// <summary>
    /// 解锁类型
    /// </summary>
    public enum UnlockType
    {
        Feature,
        Recipe,
        Character,
        Area,
        Skill,
        Title
    }

    /// <summary>
    /// Buff 奖励
    /// </summary>
    [Serializable]
    public class BuffReward : AchievementReward
    {
        [Tooltip("Buff ID")]
        public string BuffId;

        [Tooltip("Buff 名称")]
        public string BuffName;

        [Tooltip("持续时间（秒，0=永久）")]
        public float Duration;

        [Tooltip("Buff 层数")]
        public int Stacks = 1;

        public override string Description =>
            !string.IsNullOrEmpty(BuffName)
                ? (Duration > 0 ? $"{BuffName} ({Duration}秒)" : $"{BuffName} (永久)")
                : $"Buff: {BuffId}";

        public override void Grant()
        {
            if (string.IsNullOrEmpty(BuffId)) return;

            // 需要外部 Buff 系统支持
            // BuffManager.Instance?.AddBuff(BuffId, Duration, Stacks);
        }
    }

    /// <summary>
    /// 好感度奖励
    /// </summary>
    [Serializable]
    public class RelationshipReward : AchievementReward
    {
        [Tooltip("NPC ID")]
        public string NpcId;

        [Tooltip("NPC 名称（显示用）")]
        public string NpcName;

        [Tooltip("好感度变化")]
        public int RelationshipChange = 10;

        public override string Description =>
            !string.IsNullOrEmpty(NpcName)
                ? $"{NpcName} 好感度 {(RelationshipChange >= 0 ? "+" : "")}{RelationshipChange}"
                : $"好感度 {(RelationshipChange >= 0 ? "+" : "")}{RelationshipChange}";

        public override void Grant()
        {
            if (string.IsNullOrEmpty(NpcId)) return;

            // 将由 RelationshipManager 处理
            // RelationshipManager.Instance?.AddRelationship(NpcId, RelationshipChange);
        }
    }

    /// <summary>
    /// 成就点数奖励
    /// </summary>
    [Serializable]
    public class AchievementPointReward : AchievementReward
    {
        [Tooltip("成就点数")]
        public int Points = 10;

        public override string Description => $"成就点数 +{Points}";

        public override void Grant()
        {
            if (Points <= 0) return;

            var manager = AchievementManager.Instance;
            if (manager != null)
            {
                manager.AddAchievementPoints(Points);
            }
        }
    }

    /// <summary>
    /// 自定义奖励
    /// </summary>
    [Serializable]
    public class CustomAchievementReward : AchievementReward
    {
        [Tooltip("奖励ID")]
        public string RewardId;

        [Tooltip("奖励描述")]
        public string RewardDescription;

        [Tooltip("自定义数据")]
        public string CustomData;

        public override string Description =>
            !string.IsNullOrEmpty(RewardDescription) ? RewardDescription : $"自定义: {RewardId}";

        public override void Grant()
        {
            if (string.IsNullOrEmpty(RewardId)) return;

            if (_customRewardHandlers.TryGetValue(RewardId, out var handler))
            {
                handler(CustomData);
            }
        }

        // 自定义奖励处理器注册
        private static readonly System.Collections.Generic.Dictionary<string, Action<string>> _customRewardHandlers =
            new System.Collections.Generic.Dictionary<string, Action<string>>();

        public static void RegisterCustomHandler(string id, Action<string> handler)
        {
            _customRewardHandlers[id] = handler;
        }

        public static void UnregisterCustomHandler(string id)
        {
            _customRewardHandlers.Remove(id);
        }
    }

    /// <summary>
    /// 组合奖励
    /// </summary>
    [Serializable]
    public class CompositeReward : AchievementReward
    {
        [Tooltip("子奖励列表")]
        [SerializeReference]
        public AchievementReward[] SubRewards;

        public override string Description
        {
            get
            {
                if (SubRewards == null || SubRewards.Length == 0)
                    return "无奖励";

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < SubRewards.Length; i++)
                {
                    if (SubRewards[i] != null)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(SubRewards[i].Description);
                    }
                }
                return sb.ToString();
            }
        }

        public override void Grant()
        {
            if (SubRewards == null) return;

            for (int i = 0; i < SubRewards.Length; i++)
            {
                SubRewards[i]?.Grant();
            }
        }
    }
}