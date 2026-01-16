using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Relationship;
using ZeroEngine.Inventory;

namespace ZeroEngine.Samples
{
    /// <summary>
    /// Relationship 系统示例
    /// 演示好感度管理、送礼系统和事件触发
    /// </summary>
    public class RelationshipExample : MonoBehaviour
    {
        [Header("Test NPC")]
        [SerializeField] private RelationshipDataSO testNpc;
        [SerializeField] private string testNpcId = "npc_001";

        [Header("Test Gifts")]
        [SerializeField] private InventoryItemSO lovedGift;
        [SerializeField] private InventoryItemSO likedGift;
        [SerializeField] private InventoryItemSO dislikedGift;

        private readonly List<RelationshipDataSO> _tempNpcList = new List<RelationshipDataSO>(16);
        private readonly List<RelationshipEvent> _tempEventList = new List<RelationshipEvent>(8);

        private void Start()
        {
            // 监听好感度事件
            RelationshipManager.Instance.OnRelationshipEvent += OnRelationshipEvent;

            Debug.Log("[RelationshipExample] Relationship Example Started");
            Debug.Log("[RelationshipExample] Press T to talk to NPC");
            Debug.Log("[RelationshipExample] Press 1-3 to give gifts (1=Loved, 2=Liked, 3=Disliked)");
            Debug.Log("[RelationshipExample] Press G to add test gifts to inventory");
            Debug.Log("[RelationshipExample] Press S to show status");
            Debug.Log("[RelationshipExample] Press L to list all NPCs");

            ShowNpcStatus();
        }

        private void Update()
        {
            // 对话
            if (Input.GetKeyDown(KeyCode.T))
            {
                TryTalk();
            }

            // 送礼 - 喜爱
            if (Input.GetKeyDown(KeyCode.Alpha1) && lovedGift != null)
            {
                TryGiveGift(lovedGift, "Loved");
            }

            // 送礼 - 喜欢
            if (Input.GetKeyDown(KeyCode.Alpha2) && likedGift != null)
            {
                TryGiveGift(likedGift, "Liked");
            }

            // 送礼 - 讨厌
            if (Input.GetKeyDown(KeyCode.Alpha3) && dislikedGift != null)
            {
                TryGiveGift(dislikedGift, "Disliked");
            }

            // 添加测试礼物
            if (Input.GetKeyDown(KeyCode.G))
            {
                AddTestGifts();
            }

            // 显示状态
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShowNpcStatus();
            }

            // 列出所有 NPC
            if (Input.GetKeyDown(KeyCode.L))
            {
                ListAllNpcs();
            }

            // 增加好感度 (调试)
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                AddPoints(10);
            }

            // 减少好感度 (调试)
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                AddPoints(-10);
            }

            // 检查可用事件
            if (Input.GetKeyDown(KeyCode.E))
            {
                CheckAvailableEvents();
            }
        }

        private void TryTalk()
        {
            string npcId = GetCurrentNpcId();
            bool success = RelationshipManager.Instance.TryTalk(npcId);

            if (success)
            {
                Debug.Log($"[RelationshipExample] Talked to NPC: {npcId}");
            }
            else
            {
                Debug.Log($"[RelationshipExample] Cannot talk to NPC (daily limit reached?)");
            }
        }

        private void TryGiveGift(InventoryItemSO gift, string giftType)
        {
            string npcId = GetCurrentNpcId();
            bool success = RelationshipManager.Instance.TryGiveGift(npcId, gift);

            if (success)
            {
                Debug.Log($"[RelationshipExample] Gave {giftType} gift: {gift.ItemName}");
            }
            else
            {
                Debug.Log($"[RelationshipExample] Cannot give gift (no item or daily limit?)");
            }
        }

        private void AddTestGifts()
        {
            if (lovedGift != null)
            {
                InventoryManager.Instance?.AddItem(lovedGift, 5);
                Debug.Log($"[RelationshipExample] Added {lovedGift.ItemName} x5");
            }
            if (likedGift != null)
            {
                InventoryManager.Instance?.AddItem(likedGift, 5);
                Debug.Log($"[RelationshipExample] Added {likedGift.ItemName} x5");
            }
            if (dislikedGift != null)
            {
                InventoryManager.Instance?.AddItem(dislikedGift, 5);
                Debug.Log($"[RelationshipExample] Added {dislikedGift.ItemName} x5");
            }
        }

        private void AddPoints(int points)
        {
            string npcId = GetCurrentNpcId();
            RelationshipManager.Instance.AddPoints(npcId, points, true);
            Debug.Log($"[RelationshipExample] Added {points} points to {npcId}");
        }

        private void ShowNpcStatus()
        {
            string npcId = GetCurrentNpcId();
            var progress = RelationshipManager.Instance.GetProgress(npcId);
            var npcData = RelationshipManager.Instance.GetNpcData(npcId);

            if (progress == null)
            {
                Debug.Log($"[RelationshipExample] No data for NPC: {npcId}");
                return;
            }

            string npcName = npcData?.DisplayName ?? npcId;

            Debug.Log($"[RelationshipExample] === {npcName} Status ===");
            Debug.Log($"  Level: {progress.Level}");
            Debug.Log($"  Points: {progress.Points}");
            Debug.Log($"  Gifts Today: {progress.GiftCountToday}/{npcData?.MaxGiftsPerDay ?? 1}");
            Debug.Log($"  Talks Today: {progress.TalkCountToday}/{npcData?.MaxTalksPerDay ?? 1}");

            // 显示到下一等级的进度
            if (npcData != null)
            {
                int nextLevelPoints = npcData.GetPointsForNextLevel(progress.Level);
                if (nextLevelPoints > 0)
                {
                    Debug.Log($"  Next Level: {nextLevelPoints - progress.Points} points needed");
                }
            }

            // 显示已触发事件
            if (progress.TriggeredEvents.Count > 0)
            {
                Debug.Log($"  Triggered Events: {string.Join(", ", progress.TriggeredEvents)}");
            }
        }

        private void ListAllNpcs()
        {
            Debug.Log("[RelationshipExample] === All NPCs ===");

            var allNpcs = RelationshipManager.Instance.GetAllNpcs();
            foreach (var npc in allNpcs)
            {
                if (npc == null) continue;

                var level = RelationshipManager.Instance.GetLevel(npc.NpcId);
                var points = RelationshipManager.Instance.GetPoints(npc.NpcId);

                string levelIcon = level switch
                {
                    RelationshipLevel.Stranger => "👤",
                    RelationshipLevel.Acquaintance => "🤝",
                    RelationshipLevel.Friend => "😊",
                    RelationshipLevel.CloseFriend => "😄",
                    RelationshipLevel.BestFriend => "🌟",
                    RelationshipLevel.Lover => "❤️",
                    RelationshipLevel.Partner => "💍",
                    _ => "?"
                };

                Debug.Log($"  {levelIcon} [{npc.NpcType}] {npc.DisplayName} - {level} ({points} pts)");
            }

            // 按类型统计
            Debug.Log("[RelationshipExample] === By Type ===");
            foreach (NpcType type in System.Enum.GetValues(typeof(NpcType)))
            {
                RelationshipManager.Instance.GetNpcsByType(type, _tempNpcList);
                if (_tempNpcList.Count > 0)
                {
                    Debug.Log($"  {type}: {_tempNpcList.Count} NPCs");
                }
            }
        }

        private void CheckAvailableEvents()
        {
            string npcId = GetCurrentNpcId();
            RelationshipManager.Instance.GetAvailableEvents(npcId, _tempEventList);

            Debug.Log($"[RelationshipExample] === Available Events for {npcId} ===");

            if (_tempEventList.Count == 0)
            {
                Debug.Log("  No events available");
            }
            else
            {
                foreach (var evt in _tempEventList)
                {
                    Debug.Log($"  {evt.EventId}: {evt.DisplayName} (Requires: {evt.RequiredLevel})");
                }
            }
        }

        private string GetCurrentNpcId()
        {
            return testNpc != null ? testNpc.NpcId : testNpcId;
        }

        private void OnRelationshipEvent(RelationshipEventArgs args)
        {
            switch (args.Type)
            {
                case RelationshipEventType.PointsChanged:
                    Debug.Log($"[RelationshipEvent] {args.NpcName}: {args.OldPoints} -> {args.NewPoints}");
                    break;

                case RelationshipEventType.LevelUp:
                    Debug.Log($"[RelationshipEvent] ⬆️ {args.NpcName} LEVEL UP: {args.OldLevel} -> {args.NewLevel}");
                    break;

                case RelationshipEventType.LevelDown:
                    Debug.Log($"[RelationshipEvent] ⬇️ {args.NpcName} LEVEL DOWN: {args.OldLevel} -> {args.NewLevel}");
                    break;

                case RelationshipEventType.GiftReceived:
                    string reaction = args.GiftPreference switch
                    {
                        GiftPreference.Loved => "😍 LOVES IT!",
                        GiftPreference.Liked => "😊 Likes it",
                        GiftPreference.Neutral => "😐 Neutral",
                        GiftPreference.Disliked => "😕 Dislikes it",
                        GiftPreference.Hated => "😡 HATES IT!",
                        _ => "?"
                    };
                    Debug.Log($"[RelationshipEvent] 🎁 {args.NpcName} received gift: {reaction} ({args.PointsChange:+#;-#;0})");
                    break;

                case RelationshipEventType.EventTriggered:
                    Debug.Log($"[RelationshipEvent] 📜 Event triggered: {args.EventId}");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (RelationshipManager.Instance != null)
            {
                RelationshipManager.Instance.OnRelationshipEvent -= OnRelationshipEvent;
            }
        }

        // ============================================================
        // 使用说明：
        // 1. 创建 RelationshipDataSO (Create > ZeroEngine > Relationship > NPC Data)
        // 2. 配置 NPC 信息、等级阈值、礼物偏好
        // 3. 将 NPC 数据添加到 RelationshipManager 的列表中
        // 4. 准备礼物物品 (InventoryItemSO)
        // 5. 运行场景，按 T 对话，按 1-3 送礼
        // ============================================================
    }
}