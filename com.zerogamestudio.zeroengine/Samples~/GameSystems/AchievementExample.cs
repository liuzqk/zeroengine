using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Achievement;

namespace ZeroEngine.Samples
{
    /// <summary>
    /// Achievement 系统示例
    /// 演示成就追踪、事件触发和奖励领取
    /// </summary>
    public class AchievementExample : MonoBehaviour
    {
        [Header("Test Achievements")]
        [SerializeField] private AchievementSO testAchievement;
        [SerializeField] private AchievementGroupSO testGroup;

        [Header("Statistics")]
        [SerializeField] private int killCount;
        [SerializeField] private int collectCount;

        private readonly List<AchievementSO> _tempList = new List<AchievementSO>(16);

        private void Start()
        {
            // 监听成就事件
            AchievementManager.Instance.OnAchievementEvent += OnAchievementEvent;

            Debug.Log("[AchievementExample] Achievement Example Started");
            Debug.Log("[AchievementExample] Press K to simulate Kill event");
            Debug.Log("[AchievementExample] Press C to simulate Collect event");
            Debug.Log("[AchievementExample] Press R to claim all rewards");
            Debug.Log("[AchievementExample] Press P to show progress");

            ShowStatistics();
        }

        private void Update()
        {
            // 模拟击杀事件
            if (Input.GetKeyDown(KeyCode.K))
            {
                killCount++;
                AchievementManager.Instance.TriggerEvent("Kill");
                Debug.Log($"[AchievementExample] Kill event triggered! Total kills: {killCount}");
            }

            // 模拟收集事件
            if (Input.GetKeyDown(KeyCode.C))
            {
                collectCount++;
                AchievementManager.Instance.TriggerEvent("Collect");
                Debug.Log($"[AchievementExample] Collect event triggered! Total collected: {collectCount}");
            }

            // 模拟批量击杀（传递数量）
            if (Input.GetKeyDown(KeyCode.M))
            {
                int multiKill = 5;
                killCount += multiKill;
                AchievementManager.Instance.TriggerEvent("Kill", multiKill);
                Debug.Log($"[AchievementExample] Multi-kill x{multiKill}! Total kills: {killCount}");
            }

            // 领取所有奖励
            if (Input.GetKeyDown(KeyCode.R))
            {
                int claimed = AchievementManager.Instance.ClaimAllRewards();
                Debug.Log($"[AchievementExample] Claimed {claimed} rewards!");
            }

            // 显示进度
            if (Input.GetKeyDown(KeyCode.P))
            {
                ShowProgress();
            }

            // 显示统计
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShowStatistics();
            }
        }

        private void ShowProgress()
        {
            Debug.Log("[AchievementExample] === Achievement Progress ===");

            if (testAchievement != null)
            {
                var state = AchievementManager.Instance.GetState(testAchievement);
                var progress = AchievementManager.Instance.GetProgress(testAchievement);

                Debug.Log($"[AchievementExample] {testAchievement.DisplayName}:");
                Debug.Log($"  State: {state}");
                Debug.Log($"  Progress: {progress:P0}");
            }

            // 显示所有成就
            var allAchievements = AchievementManager.Instance.GetAllAchievements();
            foreach (var achievement in allAchievements)
            {
                if (achievement == null) continue;

                var state = AchievementManager.Instance.GetState(achievement);
                var progress = AchievementManager.Instance.GetProgress(achievement);

                string stateIcon = state switch
                {
                    AchievementState.Locked => "🔒",
                    AchievementState.InProgress => "⏳",
                    AchievementState.Completed => "✓",
                    AchievementState.Claimed => "🏆",
                    _ => "?"
                };

                Debug.Log($"  {stateIcon} {achievement.DisplayName} - {progress:P0}");
            }
        }

        private void ShowStatistics()
        {
            Debug.Log("[AchievementExample] === Statistics ===");
            Debug.Log($"  Total Achievements: {AchievementManager.Instance.TotalCount}");
            Debug.Log($"  Completed: {AchievementManager.Instance.CompletedCount}");
            Debug.Log($"  Total Points: {AchievementManager.Instance.TotalPoints}");

            // 按分类统计
            foreach (AchievementCategory category in System.Enum.GetValues(typeof(AchievementCategory)))
            {
                AchievementManager.Instance.GetAchievementsByCategory(category, _tempList);
                if (_tempList.Count > 0)
                {
                    int completed = 0;
                    foreach (var ach in _tempList)
                    {
                        if (AchievementManager.Instance.IsCompleted(ach))
                            completed++;
                    }
                    Debug.Log($"  {category}: {completed}/{_tempList.Count}");
                }
            }
        }

        private void OnAchievementEvent(AchievementEventArgs args)
        {
            switch (args.Type)
            {
                case AchievementEventType.Unlocked:
                    Debug.Log($"[AchievementEvent] 🎉 ACHIEVEMENT UNLOCKED: {args.Achievement?.DisplayName}");
                    Debug.Log($"  Points: +{args.Achievement?.Points}");
                    break;

                case AchievementEventType.ProgressUpdated:
                    Debug.Log($"[AchievementEvent] Progress: {args.Achievement?.DisplayName} - {args.Progress:P0}");
                    break;

                case AchievementEventType.RewardClaimed:
                    Debug.Log($"[AchievementEvent] 🎁 Reward claimed for: {args.Achievement?.DisplayName}");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementEvent -= OnAchievementEvent;
            }
        }

        // ============================================================
        // 使用说明：
        // 1. 创建 AchievementSO 资源 (Create > ZeroEngine > Achievement > Achievement)
        // 2. 配置成就条件（如 CounterCondition 监听 "Kill" 事件）
        // 3. 配置成就奖励（如 ItemReward, AchievementPointReward）
        // 4. 将成就添加到 AchievementManager 的列表中
        // 5. 运行场景，按 K/C 触发事件，观察成就进度
        // ============================================================
    }
}