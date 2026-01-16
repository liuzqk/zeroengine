using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Crafting;
using ZeroEngine.Inventory;

namespace ZeroEngine.Samples
{
    /// <summary>
    /// Crafting 系统示例
    /// 演示配方解锁、材料检查和合成操作
    /// </summary>
    public class CraftingExample : MonoBehaviour
    {
        [Header("Test Recipes")]
        [SerializeField] private CraftingRecipeSO testRecipe;
        [SerializeField] private RecipeBookSO testRecipeBook;

        [Header("Test Materials")]
        [SerializeField] private InventoryItemSO material1;
        [SerializeField] private InventoryItemSO material2;

        [Header("Settings")]
        [SerializeField] private string currentWorkbench = "BasicWorkbench";

        private readonly List<CraftingRecipeSO> _tempRecipes = new List<CraftingRecipeSO>(16);

        private void Start()
        {
            // 监听合成事件
            CraftingManager.Instance.OnCraftingEvent += OnCraftingEvent;

            Debug.Log("[CraftingExample] Crafting Example Started");
            Debug.Log("[CraftingExample] Press C to craft test recipe");
            Debug.Log("[CraftingExample] Press U to unlock test recipe");
            Debug.Log("[CraftingExample] Press A to add test materials");
            Debug.Log("[CraftingExample] Press L to list all recipes");
            Debug.Log("[CraftingExample] Press S to show skills");

            ShowRecipeStatus();
        }

        private void Update()
        {
            // 尝试合成
            if (Input.GetKeyDown(KeyCode.C) && testRecipe != null)
            {
                TryCraft();
            }

            // 解锁配方
            if (Input.GetKeyDown(KeyCode.U) && testRecipe != null)
            {
                UnlockRecipe();
            }

            // 添加测试材料
            if (Input.GetKeyDown(KeyCode.A))
            {
                AddTestMaterials();
            }

            // 列出所有配方
            if (Input.GetKeyDown(KeyCode.L))
            {
                ListRecipes();
            }

            // 显示技能
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShowSkills();
            }

            // 批量合成
            if (Input.GetKeyDown(KeyCode.B) && testRecipe != null)
            {
                TryCraftBatch(3);
            }

            // 取消合成
            if (Input.GetKeyDown(KeyCode.X))
            {
                CancelAllCrafting();
            }
        }

        private void TryCraft()
        {
            // 检查是否可以合成
            var canCraft = CraftingManager.Instance.CanCraft(testRecipe, currentWorkbench);

            if (canCraft != CraftingResult.Success)
            {
                Debug.Log($"[CraftingExample] Cannot craft: {canCraft}");
                return;
            }

            // 执行合成
            var result = CraftingManager.Instance.StartCraft(testRecipe, currentWorkbench);
            Debug.Log($"[CraftingExample] Craft result: {result}");
        }

        private void TryCraftBatch(int count)
        {
            var canCraft = CraftingManager.Instance.CanCraft(testRecipe, currentWorkbench, count);

            if (canCraft != CraftingResult.Success)
            {
                Debug.Log($"[CraftingExample] Cannot batch craft x{count}: {canCraft}");
                return;
            }

            var result = CraftingManager.Instance.StartCraft(testRecipe, currentWorkbench, count);
            Debug.Log($"[CraftingExample] Batch craft x{count} result: {result}");
        }

        private void UnlockRecipe()
        {
            bool unlocked = CraftingManager.Instance.TryUnlockRecipe(testRecipe);
            Debug.Log($"[CraftingExample] Recipe unlock: {(unlocked ? "SUCCESS" : "FAILED")}");
        }

        private void AddTestMaterials()
        {
            if (material1 != null)
            {
                InventoryManager.Instance?.AddItem(material1, 10);
                Debug.Log($"[CraftingExample] Added {material1.ItemName} x10");
            }

            if (material2 != null)
            {
                InventoryManager.Instance?.AddItem(material2, 10);
                Debug.Log($"[CraftingExample] Added {material2.ItemName} x10");
            }
        }

        private void ListRecipes()
        {
            Debug.Log("[CraftingExample] === All Recipes ===");

            var allRecipes = CraftingManager.Instance.GetAllRecipes();
            foreach (var recipe in allRecipes)
            {
                if (recipe == null) continue;

                bool unlocked = CraftingManager.Instance.IsUnlocked(recipe);
                string status = unlocked ? "✓" : "🔒";
                Debug.Log($"  {status} [{recipe.Category}] {recipe.DisplayName}");
            }

            Debug.Log("[CraftingExample] === Unlocked Recipes ===");
            CraftingManager.Instance.GetUnlockedRecipes(_tempRecipes);
            foreach (var recipe in _tempRecipes)
            {
                Debug.Log($"  ✓ {recipe.DisplayName}");
            }
        }

        private void ShowSkills()
        {
            Debug.Log("[CraftingExample] === Crafting Skills ===");

            // 显示常见技能
            string[] skillIds = { "Smithing", "Alchemy", "Cooking", "Tailoring" };
            foreach (var skillId in skillIds)
            {
                var data = CraftingManager.Instance.GetSkillData(skillId);
                if (data != null)
                {
                    Debug.Log($"  {skillId}: Lv.{data.Level} ({data.CurrentExp}/{GetExpForLevel(data.Level)})");
                }
            }
        }

        private int GetExpForLevel(int level)
        {
            return level * 100; // 简化的经验公式
        }

        private void ShowRecipeStatus()
        {
            if (testRecipe == null)
            {
                Debug.Log("[CraftingExample] No test recipe assigned");
                return;
            }

            Debug.Log($"[CraftingExample] Test Recipe: {testRecipe.DisplayName}");
            Debug.Log($"  Category: {testRecipe.Category}");
            Debug.Log($"  Unlock Type: {testRecipe.UnlockType}");
            Debug.Log($"  Craft Time: {testRecipe.CraftTime}s");
            Debug.Log($"  Success Rate: {testRecipe.SuccessRate:P0}");

            bool unlocked = CraftingManager.Instance.IsUnlocked(testRecipe);
            Debug.Log($"  Status: {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }

        private void CancelAllCrafting()
        {
            var activeProgress = CraftingManager.Instance.GetActiveProgress();
            int cancelled = 0;

            for (int i = activeProgress.Count - 1; i >= 0; i--)
            {
                if (CraftingManager.Instance.CancelCraft(i))
                {
                    cancelled++;
                }
            }

            Debug.Log($"[CraftingExample] Cancelled {cancelled} crafting operations");
        }

        private void OnCraftingEvent(CraftingEventArgs args)
        {
            switch (args.Type)
            {
                case CraftingEventType.Started:
                    Debug.Log($"[CraftingEvent] ⚒️ Started crafting: {args.Recipe?.DisplayName} x{args.BatchCount}");
                    break;

                case CraftingEventType.Completed:
                    string resultIcon = args.Result == CraftingResult.GreatSuccess ? "⭐" : "✓";
                    Debug.Log($"[CraftingEvent] {resultIcon} Crafting complete: {args.Recipe?.DisplayName}");
                    break;

                case CraftingEventType.Failed:
                    Debug.Log($"[CraftingEvent] ❌ Crafting failed: {args.Recipe?.DisplayName}");
                    break;

                case CraftingEventType.Cancelled:
                    Debug.Log($"[CraftingEvent] ⏹️ Crafting cancelled: {args.Recipe?.DisplayName}");
                    break;

                case CraftingEventType.RecipeUnlocked:
                    Debug.Log($"[CraftingEvent] 📖 Recipe unlocked: {args.Recipe?.DisplayName}");
                    break;

                case CraftingEventType.SkillLevelUp:
                    Debug.Log($"[CraftingEvent] 📈 Skill level up: {args.SkillId} -> Lv.{args.SkillLevel}");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (CraftingManager.Instance != null)
            {
                CraftingManager.Instance.OnCraftingEvent -= OnCraftingEvent;
            }
        }

        // ============================================================
        // 使用说明：
        // 1. 创建 CraftingRecipeSO (Create > ZeroEngine > Crafting > Recipe)
        // 2. 配置材料 (Ingredients) 和产出 (Outputs)
        // 3. 设置解锁方式 (UnlockType)
        // 4. 将配方添加到 CraftingManager 的列表中
        // 5. 运行场景，按 A 添加材料，按 C 合成
        // ============================================================
    }
}