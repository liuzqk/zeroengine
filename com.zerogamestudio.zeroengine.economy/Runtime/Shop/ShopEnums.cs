using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Shop
{
    /// <summary>商店类型</summary>
    public enum ShopType
    {
        General,        // 通用商店
        Weapon,         // 武器店
        Armor,          // 防具店
        Consumable,     // 消耗品店
        Material,       // 材料店
        Special,        // 特殊商店
        BlackMarket,    // 黑市
        Event           // 活动商店
    }

    /// <summary>货币类型</summary>
    public enum ShopCurrencyType
    {
        Gold,           // 金币
        Premium,        // 付费货币
        Honor,          // 荣誉点
        Guild,          // 公会货币
        Event,          // 活动货币
        Custom          // 自定义
    }

    /// <summary>购买结果</summary>
    public enum PurchaseResult
    {
        Success,
        InsufficientCurrency,
        InsufficientStock,
        NotAvailable,
        LevelTooLow,
        ReputationTooLow,
        AlreadyOwned,
        InventoryFull,
        LimitReached,
        ShopClosed,
        Error
    }

    /// <summary>补货类型</summary>
    public enum RestockType
    {
        Never,          // 永不补货
        Daily,          // 每日补货
        Weekly,         // 每周补货
        OnVisit,        // 每次访问补货
        Manual          // 手动补货
    }

    /// <summary>折扣类型</summary>
    public enum DiscountType
    {
        None,
        Percentage,     // 百分比折扣
        FlatAmount,     // 固定金额减免
        BuyOneGetOne,   // 买一送一
        Bundle          // 捆绑优惠
    }

    /// <summary>商店事件类型</summary>
    public enum ShopEventType
    {
        Opened,
        Closed,
        ItemPurchased,
        ItemSold,
        Restocked,
        DiscountApplied,
        LimitReached
    }

    /// <summary>商品价格</summary>
    [Serializable]
    public class ShopPrice
    {
        public ShopCurrencyType CurrencyType = ShopCurrencyType.Gold;
        public string CustomCurrencyId;
        public int Amount = 100;

        public string GetCurrencyId()
        {
            return CurrencyType == ShopCurrencyType.Custom ? CustomCurrencyId : CurrencyType.ToString();
        }
    }

    /// <summary>商品限购配置</summary>
    [Serializable]
    public class PurchaseLimit
    {
        [Tooltip("限购数量 (0=无限)")]
        public int MaxCount;

        [Tooltip("限购周期")]
        public RestockType ResetPeriod = RestockType.Never;

        [Tooltip("已购买数量")]
        [HideInInspector]
        public int CurrentCount;
    }

    /// <summary>商品库存配置</summary>
    [Serializable]
    public class StockConfig
    {
        [Tooltip("初始库存 (-1=无限)")]
        public int InitialStock = -1;

        [Tooltip("当前库存")]
        [HideInInspector]
        public int CurrentStock;

        [Tooltip("补货类型")]
        public RestockType RestockType = RestockType.Never;

        [Tooltip("补货数量")]
        public int RestockAmount = 10;

        public bool HasStock => InitialStock < 0 || CurrentStock > 0;
    }

    /// <summary>折扣配置</summary>
    [Serializable]
    public class DiscountConfig
    {
        public DiscountType Type = DiscountType.None;
        public float Value;  // 百分比(0-1)或固定金额
        public string ConditionId;  // 折扣条件

        public int ApplyDiscount(int originalPrice)
        {
            return Type switch
            {
                DiscountType.Percentage => Mathf.RoundToInt(originalPrice * (1f - Value)),
                DiscountType.FlatAmount => Mathf.Max(0, originalPrice - (int)Value),
                _ => originalPrice
            };
        }
    }

    /// <summary>商店营业时间</summary>
    [Serializable]
    public class ShopSchedule
    {
        public bool AlwaysOpen = true;
        public int OpenHour = 8;
        public int CloseHour = 20;
        public bool[] OpenDays = { true, true, true, true, true, true, true }; // Sun-Sat
    }

    /// <summary>商店事件参数</summary>
    public class ShopEventArgs
    {
        public ShopEventType Type { get; private set; }
        public ShopSO Shop { get; private set; }
        public ShopItemSO Item { get; private set; }
        public int Amount { get; private set; }
        public int TotalPrice { get; private set; }
        public PurchaseResult Result { get; private set; }

        public static ShopEventArgs Opened(ShopSO shop)
            => new() { Type = ShopEventType.Opened, Shop = shop };

        public static ShopEventArgs Closed(ShopSO shop)
            => new() { Type = ShopEventType.Closed, Shop = shop };

        public static ShopEventArgs Purchased(ShopSO shop, ShopItemSO item, int amount, int totalPrice)
            => new() { Type = ShopEventType.ItemPurchased, Shop = shop, Item = item, Amount = amount, TotalPrice = totalPrice };

        public static ShopEventArgs Sold(ShopSO shop, ShopItemSO item, int amount, int totalPrice)
            => new() { Type = ShopEventType.ItemSold, Shop = shop, Item = item, Amount = amount, TotalPrice = totalPrice };

        public static ShopEventArgs Restocked(ShopSO shop)
            => new() { Type = ShopEventType.Restocked, Shop = shop };
    }
}