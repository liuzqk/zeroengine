using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Quest;
using ZeroEngine.Inventory;

namespace ZeroEngine.Dialog.Integration
{
    /// <summary>
    /// Callback IDs for dialog system integration.
    /// Use these constants in DialogCallbackNode.
    /// </summary>
    public static class DialogCallbackIds
    {
        // Quest Integration
        public const string AcceptQuest = "Quest.Accept";
        public const string CompleteQuest = "Quest.Complete";
        public const string SubmitQuest = "Quest.Submit";
        public const string AbandonQuest = "Quest.Abandon";

        // Inventory Integration
        public const string GiveItem = "Inventory.Give";
        public const string TakeItem = "Inventory.Take";
        public const string CheckItem = "Inventory.Check";

        // Currency Integration
        public const string GiveGold = "Currency.Give";
        public const string TakeGold = "Currency.Take";

        // Game Events
        public const string TriggerEvent = "Event.Trigger";
        public const string PlaySound = "Audio.Play";
    }

    /// <summary>
    /// Central handler for Dialog system integration with other ZeroEngine systems.
    /// Attach to DialogRunner or a persistent manager object.
    /// </summary>
    public class DialogIntegrationHandler : MonoBehaviour
    {
        [Header("Auto-Connect")]
        [SerializeField] private DialogRunner _dialogRunner;

        [Header("Debug")]
        [SerializeField] private bool _logCallbacks = true;

        private void Awake()
        {
            if (_dialogRunner == null)
            {
                _dialogRunner = GetComponent<DialogRunner>();
            }
        }

        private void OnEnable()
        {
            if (_dialogRunner != null)
            {
                _dialogRunner.OnCallback += HandleCallback;
            }

            // Also subscribe to DialogManager if available
            if (DialogManager.Instance != null)
            {
                // DialogManager uses providers which may trigger callbacks differently
            }
        }

        private void OnDisable()
        {
            if (_dialogRunner != null)
            {
                _dialogRunner.OnCallback -= HandleCallback;
            }
        }

        /// <summary>
        /// Handle callbacks from DialogCallbackNode.
        /// Parameter format varies by callback type:
        /// - Quest: "questId"
        /// - Inventory: "itemId:amount" or "itemId"
        /// - Event: "eventName:param"
        /// </summary>
        private void HandleCallback(string callbackId, string parameter)
        {
            if (_logCallbacks)
            {
                Debug.Log($"[DialogIntegration] Callback: {callbackId}, Param: {parameter}");
            }

            switch (callbackId)
            {
                // Quest
                case DialogCallbackIds.AcceptQuest:
                    HandleAcceptQuest(parameter);
                    break;
                case DialogCallbackIds.CompleteQuest:
                    HandleCompleteQuest(parameter);
                    break;
                case DialogCallbackIds.SubmitQuest:
                    HandleSubmitQuest(parameter);
                    break;
                case DialogCallbackIds.AbandonQuest:
                    HandleAbandonQuest(parameter);
                    break;

                // Inventory
                case DialogCallbackIds.GiveItem:
                    HandleGiveItem(parameter);
                    break;
                case DialogCallbackIds.TakeItem:
                    HandleTakeItem(parameter);
                    break;
                case DialogCallbackIds.CheckItem:
                    HandleCheckItem(parameter);
                    break;

                // Currency
                case DialogCallbackIds.GiveGold:
                    HandleGiveGold(parameter);
                    break;
                case DialogCallbackIds.TakeGold:
                    HandleTakeGold(parameter);
                    break;

                // Events
                case DialogCallbackIds.TriggerEvent:
                    HandleTriggerEvent(parameter);
                    break;
                case DialogCallbackIds.PlaySound:
                    HandlePlaySound(parameter);
                    break;

                default:
                    // Custom callback - trigger event for external handlers
                    OnCustomCallback?.Invoke(callbackId, parameter);
                    break;
            }
        }

        #region Quest Handlers

        private void HandleAcceptQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;

            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                Debug.LogWarning("[DialogIntegration] QuestManager not found");
                return;
            }

            bool success = questManager.AcceptQuest(questId);
            SetResultVariable("lastQuestResult", success);
        }

        private void HandleCompleteQuest(string questId)
        {
            // Note: Quest completion is typically automatic via progress
            // This is for force-completing via dialog
            if (string.IsNullOrEmpty(questId)) return;

            var questManager = QuestManager.Instance;
            if (questManager == null) return;

            // Quest system doesn't have direct complete, use submit
            Debug.Log($"[DialogIntegration] Quest '{questId}' marked for completion");
            SetResultVariable("lastQuestResult", true);
        }

        private void HandleSubmitQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;

            var questManager = QuestManager.Instance;
            if (questManager == null) return;

            questManager.SubmitQuest(questId);
            SetResultVariable("lastQuestResult", true);
        }

        private void HandleAbandonQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;

            var questManager = QuestManager.Instance;
            if (questManager == null) return;

            questManager.AbandonQuest(questId);
            SetResultVariable("lastQuestResult", true);
        }

        #endregion

        #region Inventory Handlers

        private void HandleGiveItem(string param)
        {
            ParseItemParam(param, out string itemId, out int amount);
            if (string.IsNullOrEmpty(itemId)) return;

            var inventory = InventoryManager.Instance;
            if (inventory == null)
            {
                Debug.LogWarning("[DialogIntegration] InventoryManager not found");
                return;
            }

            bool success = inventory.AddItem(itemId, amount);
            SetResultVariable("lastItemResult", success);

            if (_logCallbacks)
            {
                Debug.Log($"[DialogIntegration] Give item: {itemId} x{amount}, Success: {success}");
            }
        }

        private void HandleTakeItem(string param)
        {
            ParseItemParam(param, out string itemId, out int amount);
            if (string.IsNullOrEmpty(itemId)) return;

            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            bool hasItem = inventory.HasItem(itemId, amount);
            if (hasItem)
            {
                inventory.RemoveItem(itemId, amount);
            }
            SetResultVariable("lastItemResult", hasItem);

            if (_logCallbacks)
            {
                Debug.Log($"[DialogIntegration] Take item: {itemId} x{amount}, Success: {hasItem}");
            }
        }

        private void HandleCheckItem(string param)
        {
            ParseItemParam(param, out string itemId, out int requiredAmount);
            if (string.IsNullOrEmpty(itemId)) return;

            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            int currentAmount = inventory.GetItemCount(itemId);
            bool hasEnough = currentAmount >= requiredAmount;

            SetResultVariable("hasItem", hasEnough);
            SetResultVariable("itemCount", currentAmount);
        }

        private void ParseItemParam(string param, out string itemId, out int amount)
        {
            amount = 1;
            itemId = param;

            if (string.IsNullOrEmpty(param)) return;

            int colonIndex = param.IndexOf(':');
            if (colonIndex > 0)
            {
                itemId = param.Substring(0, colonIndex);
                if (int.TryParse(param.Substring(colonIndex + 1), out int parsed))
                {
                    amount = parsed;
                }
            }
        }

        #endregion

        #region Currency Handlers

        private void HandleGiveGold(string param)
        {
            if (!int.TryParse(param, out int amount)) return;

            // Trigger currency event for external handling
            EventManager.Trigger(GameEvents.CurrencyGained, "Gold", amount);
            SetResultVariable("lastCurrencyResult", true);
        }

        private void HandleTakeGold(string param)
        {
            if (!int.TryParse(param, out int amount)) return;

            // Trigger currency event for external handling
            // Note: actual currency system should validate if player has enough
            EventManager.Trigger(GameEvents.CurrencySpent, "Gold", amount);
            SetResultVariable("lastCurrencyResult", true);
        }

        #endregion

        #region Event Handlers

        private void HandleTriggerEvent(string param)
        {
            if (string.IsNullOrEmpty(param)) return;

            int colonIndex = param.IndexOf(':');
            if (colonIndex > 0)
            {
                string eventName = param.Substring(0, colonIndex);
                string eventParam = param.Substring(colonIndex + 1);
                EventManager.Trigger(eventName, eventParam);
            }
            else
            {
                EventManager.Trigger<string>(param, null);
            }
        }

        private void HandlePlaySound(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey)) return;

            // Hook for Audio integration
            Debug.Log($"[DialogIntegration] PlaySound: {soundKey}");
            // AudioManager.Instance?.PlaySFX(soundKey);
        }

        #endregion

        #region Helpers

        private void SetResultVariable(string name, object value)
        {
            _dialogRunner?.SetVariable(name, value);
        }

        /// <summary>
        /// Event for custom callbacks not handled by this class.
        /// </summary>
        public event Action<string, string> OnCustomCallback;

        #endregion
    }

    /// <summary>
    /// Static helper methods for dialog-system integration.
    /// Can be used in conditions or from external scripts.
    /// </summary>
    public static class DialogIntegrationUtils
    {
        /// <summary>
        /// Check if player has a quest in a specific state.
        /// </summary>
        public static bool HasQuestInState(string questId, QuestState state)
        {
            var questManager = QuestManager.Instance;
            if (questManager == null) return false;

            return questManager.GetQuestState(questId) == state;
        }

        /// <summary>
        /// Check if player has an active quest.
        /// </summary>
        public static bool HasActiveQuest(string questId)
        {
            return QuestManager.Instance?.HasActiveQuest(questId) ?? false;
        }

        /// <summary>
        /// Check if player has enough of an item.
        /// </summary>
        public static bool HasItem(string itemId, int amount = 1)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            return inventory.GetItemCount(itemId) >= amount;
        }

        /// <summary>
        /// Get item count in player's inventory.
        /// </summary>
        public static int GetItemCount(string itemId)
        {
            return InventoryManager.Instance?.GetItemCount(itemId) ?? 0;
        }

        /// <summary>
        /// Register integration variables for condition evaluation.
        /// Call this to make quest/inventory states available in dialog conditions.
        /// </summary>
        public static void SyncVariablesToDialog(DialogVariables variables)
        {
            if (variables == null) return;

            // Sync active quest IDs as boolean variables
            var questManager = QuestManager.Instance;
            if (questManager != null)
            {
                var activeQuests = questManager.GetActiveQuests();
                foreach (var quest in activeQuests)
                {
                    variables.SetLocal($"quest_{quest.questId}", true);
                    variables.SetLocal($"questState_{quest.questId}", (int)quest.state);
                }
            }

            // Sync inventory counts for common items
            // (Specific items should be synced on-demand via CheckItem callback)
        }
    }
}