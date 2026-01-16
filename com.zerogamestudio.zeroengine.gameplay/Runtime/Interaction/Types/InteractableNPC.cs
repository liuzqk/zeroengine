using System;
using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// NPC 可交互对象 (v1.14.0+)
    /// 用于触发对话、商店、任务等
    /// </summary>
    public class InteractableNPC : InteractableBase
    {
        #region Enums

        /// <summary>NPC 交互模式</summary>
        public enum NPCInteractionMode
        {
            Dialog,         // 对话
            Shop,           // 商店
            Quest,          // 任务
            Craft,          // 制作
            Custom          // 自定义
        }

        #endregion

        #region Serialized Fields

        [Header("NPC Settings")]
        [SerializeField]
        [Tooltip("NPC ID (用于任务/好感度系统)")]
        private string _npcId;

        [SerializeField]
        [Tooltip("交互模式")]
        private NPCInteractionMode _interactionMode = NPCInteractionMode.Dialog;

        [Header("Dialog")]
        [SerializeField]
        [Tooltip("对话数据 (DialogGraphSO)")]
        private ScriptableObject _dialogData;

        [SerializeField]
        [Tooltip("对话 ID (如果使用 DialogManager)")]
        private string _dialogId;

        [Header("Shop")]
        [SerializeField]
        [Tooltip("商店数据 (ShopDataSO)")]
        private ScriptableObject _shopData;

        [Header("Quest")]
        [SerializeField]
        [Tooltip("提供的任务 ID 列表")]
        private string[] _availableQuestIds;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("交互时面向玩家")]
        private bool _facePlayerOnInteract = true;

        [SerializeField]
        [Tooltip("面向速度")]
        private float _faceSpeed = 5f;

        [SerializeField]
        [Tooltip("任务标记 (有任务时显示)")]
        private GameObject _questMarker;

        [SerializeField]
        [Tooltip("对话标记 (可对话时显示)")]
        private GameObject _dialogMarker;

        #endregion

        #region Private Fields

        private Transform _currentInteractor;
        private bool _isFacing;

        #endregion

        #region Properties

        /// <summary>NPC ID</summary>
        public string NpcId => !string.IsNullOrEmpty(_npcId) ? _npcId : InteractableId;

        /// <summary>交互模式</summary>
        public NPCInteractionMode InteractionMode => _interactionMode;

        /// <summary>对话数据</summary>
        public ScriptableObject DialogData => _dialogData;

        /// <summary>商店数据</summary>
        public ScriptableObject ShopData => _shopData;

        #endregion

        #region Events

        /// <summary>对话开始</summary>
        public event Action<InteractableNPC, GameObject> OnDialogStarted;

        /// <summary>商店打开</summary>
        public event Action<InteractableNPC> OnShopOpened;

        /// <summary>任务交互</summary>
        public event Action<InteractableNPC, string> OnQuestInteracted;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = InteractionType.Talk;

            // 根据模式更新类型
            UpdateInteractionType();
        }

        private void Start()
        {
            UpdateMarkers();
        }

        private void Update()
        {
            if (_isFacing && _currentInteractor != null)
            {
                FaceTarget(_currentInteractor.position);
            }
        }

        #endregion

        #region Interaction

        public override string GetInteractionHint()
        {
            return _interactionMode switch
            {
                NPCInteractionMode.Dialog => $"[E] Talk to {DisplayName}",
                NPCInteractionMode.Shop => $"[E] Trade with {DisplayName}",
                NPCInteractionMode.Quest => HasAvailableQuest() ? $"[E] Accept Quest from {DisplayName}" : $"[E] Talk to {DisplayName}",
                NPCInteractionMode.Craft => $"[E] Craft with {DisplayName}",
                _ => base.GetInteractionHint()
            };
        }

        protected override InteractionResult ExecuteInteraction(InteractionContext ctx)
        {
            _currentInteractor = ctx.Interactor?.transform;

            // 面向玩家
            if (_facePlayerOnInteract && _currentInteractor != null)
            {
                _isFacing = true;
            }

            // 根据模式执行不同逻辑
            return _interactionMode switch
            {
                NPCInteractionMode.Dialog => ExecuteDialog(ctx),
                NPCInteractionMode.Shop => ExecuteShop(ctx),
                NPCInteractionMode.Quest => ExecuteQuest(ctx),
                NPCInteractionMode.Craft => ExecuteCraft(ctx),
                _ => ExecuteCustom(ctx)
            };
        }

        private InteractionResult ExecuteDialog(InteractionContext ctx)
        {
            OnDialogStarted?.Invoke(this, ctx.Interactor);

            // 与 Dialog 系统集成
#if ZEROENGINE_DIALOG
            if (_dialogData != null)
            {
                var dialogManager = ZeroEngine.Dialog.DialogManager.Instance;
                if (dialogManager != null)
                {
                    dialogManager.StartDialog(_dialogData);
                    return InteractionResult.Succeeded(this, _dialogData);
                }
            }
            else if (!string.IsNullOrEmpty(_dialogId))
            {
                var dialogManager = ZeroEngine.Dialog.DialogManager.Instance;
                if (dialogManager != null)
                {
                    dialogManager.StartDialog(_dialogId);
                    return InteractionResult.Succeeded(this, _dialogId);
                }
            }
#endif
            return InteractionResult.Succeeded(this);
        }

        private InteractionResult ExecuteShop(InteractionContext ctx)
        {
            OnShopOpened?.Invoke(this);

            // 与 Shop 系统集成
#if ZEROENGINE_SHOP
            if (_shopData != null)
            {
                var shopManager = ZeroEngine.Shop.ShopManager.Instance;
                if (shopManager != null)
                {
                    shopManager.OpenShop(_shopData);
                    return InteractionResult.Succeeded(this, _shopData);
                }
            }
#endif
            return InteractionResult.Succeeded(this);
        }

        private InteractionResult ExecuteQuest(InteractionContext ctx)
        {
            // 查找可用任务
            string questId = GetFirstAvailableQuestId();
            if (!string.IsNullOrEmpty(questId))
            {
                OnQuestInteracted?.Invoke(this, questId);

#if ZEROENGINE_QUEST
                var questManager = ZeroEngine.Quest.QuestManager.Instance;
                if (questManager != null)
                {
                    questManager.StartQuest(questId);
                    return InteractionResult.Succeeded(this, questId);
                }
#endif
            }

            // 没有可用任务，回退到对话
            return ExecuteDialog(ctx);
        }

        private InteractionResult ExecuteCraft(InteractionContext ctx)
        {
            // 与 Crafting 系统集成
#if ZEROENGINE_CRAFTING
            var craftingManager = ZeroEngine.Crafting.CraftingManager.Instance;
            if (craftingManager != null)
            {
                // 打开制作 UI
                return InteractionResult.Succeeded(this);
            }
#endif
            return InteractionResult.Succeeded(this);
        }

        protected virtual InteractionResult ExecuteCustom(InteractionContext ctx)
        {
            return InteractionResult.Succeeded(this);
        }

        #endregion

        #region Helpers

        private void UpdateInteractionType()
        {
            _interactionType = _interactionMode switch
            {
                NPCInteractionMode.Dialog => InteractionType.Talk,
                NPCInteractionMode.Shop => InteractionType.Use,
                NPCInteractionMode.Quest => InteractionType.Talk,
                NPCInteractionMode.Craft => InteractionType.Craft,
                _ => InteractionType.Custom
            };
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _faceSpeed * Time.deltaTime);

                // 检查是否完成转向
                if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
                {
                    _isFacing = false;
                }
            }
        }

        private bool HasAvailableQuest()
        {
            return !string.IsNullOrEmpty(GetFirstAvailableQuestId());
        }

        private string GetFirstAvailableQuestId()
        {
            if (_availableQuestIds == null || _availableQuestIds.Length == 0)
                return null;

#if ZEROENGINE_QUEST
            var questManager = ZeroEngine.Quest.QuestManager.Instance;
            if (questManager != null)
            {
                foreach (var questId in _availableQuestIds)
                {
                    if (questManager.CanStartQuest(questId))
                    {
                        return questId;
                    }
                }
            }
#endif
            return _availableQuestIds.Length > 0 ? _availableQuestIds[0] : null;
        }

        private void UpdateMarkers()
        {
            if (_questMarker != null)
            {
                _questMarker.SetActive(HasAvailableQuest());
            }

            if (_dialogMarker != null)
            {
                _dialogMarker.SetActive(_interactionMode == NPCInteractionMode.Dialog && !HasAvailableQuest());
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置交互模式
        /// </summary>
        public void SetInteractionMode(NPCInteractionMode mode)
        {
            _interactionMode = mode;
            UpdateInteractionType();
        }

        /// <summary>
        /// 刷新标记显示
        /// </summary>
        public void RefreshMarkers()
        {
            UpdateMarkers();
        }

        /// <summary>
        /// 结束交互 (停止面向)
        /// </summary>
        public void EndInteraction()
        {
            _isFacing = false;
            _currentInteractor = null;
        }

        #endregion
    }
}
