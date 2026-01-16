using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Loot;

namespace ZeroEngine.Samples
{
    /// <summary>
    /// Loot Table 系统示例
    /// 演示掉落表配置、掉落执行和事件监听
    /// </summary>
    public class LootTableExample : MonoBehaviour
    {
        [Header("Loot Tables")]
        [SerializeField] private LootTableSO commonLootTable;
        [SerializeField] private LootTableSO rareLootTable;
        [SerializeField] private LootTableSO bossLootTable;

        [Header("Context")]
        [SerializeField] private int playerLevel = 10;

        private readonly List<LootResult> _resultCache = new List<LootResult>(16);

        private void Start()
        {
            // 监听掉落事件
            LootTableManager.Instance.OnLootEvent += OnLootEvent;

            Debug.Log("[LootExample] Loot Table Example Started");
            Debug.Log("[LootExample] Press 1-3 to test different loot tables");
            Debug.Log("[LootExample] 1: Common, 2: Rare, 3: Boss");
        }

        private void Update()
        {
            // 测试普通掉落
            if (Input.GetKeyDown(KeyCode.Alpha1) && commonLootTable != null)
            {
                TestLootTable(commonLootTable, "Common");
            }

            // 测试稀有掉落
            if (Input.GetKeyDown(KeyCode.Alpha2) && rareLootTable != null)
            {
                TestLootTable(rareLootTable, "Rare");
            }

            // 测试 Boss 掉落
            if (Input.GetKeyDown(KeyCode.Alpha3) && bossLootTable != null)
            {
                TestLootTable(bossLootTable, "Boss");
            }

            // 重置保底
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAllPity();
            }
        }

        private void TestLootTable(LootTableSO table, string tableName)
        {
            // 创建上下文
            var context = new LootContext
            {
                CharacterLevel = playerLevel
            };

            // 执行掉落
            var results = LootTableManager.Instance.Roll(table, context);

            Debug.Log($"[LootExample] === {tableName} Loot Results ===");

            if (results.Count == 0)
            {
                Debug.Log("[LootExample] No loot dropped!");
            }
            else
            {
                foreach (var result in results)
                {
                    LogLootResult(result);
                }
            }
        }

        private void LogLootResult(LootResult result)
        {
            switch (result.ResultType)
            {
                case LootResultType.Item:
                    Debug.Log($"[LootExample] Item: {result.Item?.ItemName ?? "Unknown"} x{result.Amount}");
                    break;

                case LootResultType.Currency:
                    Debug.Log($"[LootExample] Currency: {result.CurrencyType} x{result.Amount}");
                    break;

                case LootResultType.Nothing:
                    Debug.Log("[LootExample] Nothing dropped (empty roll)");
                    break;
            }
        }

        private void ResetAllPity()
        {
            if (commonLootTable != null)
                LootTableManager.Instance.ResetAllPity(commonLootTable);
            if (rareLootTable != null)
                LootTableManager.Instance.ResetAllPity(rareLootTable);
            if (bossLootTable != null)
                LootTableManager.Instance.ResetAllPity(bossLootTable);

            Debug.Log("[LootExample] All pity counters reset!");
        }

        private void OnLootEvent(LootEventArgs args)
        {
            switch (args.Type)
            {
                case LootEventType.Rolled:
                    Debug.Log($"[LootEvent] Rolled from {args.Table?.DisplayName}, got {args.Results?.Length ?? 0} items");
                    break;

                case LootEventType.Granted:
                    Debug.Log($"[LootEvent] Loot granted: {args.Results?.Length ?? 0} items added to inventory");
                    break;

                case LootEventType.PityTriggered:
                    Debug.Log($"[LootEvent] PITY TRIGGERED! Guaranteed drop activated");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (LootTableManager.Instance != null)
            {
                LootTableManager.Instance.OnLootEvent -= OnLootEvent;
            }
        }

        // ============================================================
        // 使用说明：
        // 1. 创建 LootTableSO 资源 (Create > ZeroEngine > Loot > Loot Table)
        // 2. 配置掉落条目 (Entries)，设置物品和权重
        // 3. 将 LootTableSO 拖到本脚本的对应字段
        // 4. 运行场景，按 1-3 测试不同掉落表
        // ============================================================
    }
}