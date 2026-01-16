using System;
using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 工作台/制作站 (v1.14.0+)
    /// </summary>
    public class InteractableCraftStation : InteractableBase
    {
        #region Serialized Fields

        [Header("Craft Station Settings")]
        [SerializeField]
        [Tooltip("工作台类型")]
        private CraftStationType _stationType = CraftStationType.General;

        [SerializeField]
        [Tooltip("工作台等级 (影响可制作配方)")]
        private int _stationLevel = 1;

        [SerializeField]
        [Tooltip("可用配方书 (RecipeBookSO)")]
        private ScriptableObject[] _recipeBooks;

        [SerializeField]
        [Tooltip("解锁的配方类别")]
        private string[] _unlockedCategories;

        [Header("Interaction")]
        [SerializeField]
        [Tooltip("打开制作 UI 的方式")]
        private CraftUIMode _uiMode = CraftUIMode.OpenPanel;

        [SerializeField]
        [Tooltip("制作 UI 预制体 (如果使用 Spawn 模式)")]
        private GameObject _craftUIPrefab;

        [SerializeField]
        [Tooltip("使用时是否锁定玩家移动")]
        private bool _lockPlayerMovement = true;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("使用中的特效")]
        private GameObject _activeVFX;

        [SerializeField]
        [Tooltip("空闲状态特效")]
        private GameObject _idleVFX;

        [Header("Audio")]
        [SerializeField]
        private AudioClip _openSound;

        [SerializeField]
        private AudioClip _closeSound;

        [SerializeField]
        private AudioClip _craftingLoopSound;

        #endregion

        #region Private Fields

        private bool _isInUse;
        private GameObject _currentInteractor;
        private AudioSource _audioSource;

        #endregion

        #region Properties

        /// <summary>工作台类型</summary>
        public CraftStationType StationType => _stationType;

        /// <summary>工作台等级</summary>
        public int StationLevel => _stationLevel;

        /// <summary>是否正在使用中</summary>
        public bool IsInUse => _isInUse;

        /// <summary>配方书</summary>
        public ScriptableObject[] RecipeBooks => _recipeBooks;

        #endregion

        #region Events

        /// <summary>工作台被使用</summary>
        public event Action<InteractableCraftStation, GameObject> OnStationUsed;

        /// <summary>工作台被关闭</summary>
        public event Action<InteractableCraftStation> OnStationClosed;

        /// <summary>制作开始</summary>
        public event Action<InteractableCraftStation, string> OnCraftStarted;

        /// <summary>制作完成</summary>
        public event Action<InteractableCraftStation, string, bool> OnCraftCompleted;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = InteractionType.Craft;
            _audioSource = GetComponent<AudioSource>();

            // 初始化特效状态
            if (_idleVFX != null) _idleVFX.SetActive(true);
            if (_activeVFX != null) _activeVFX.SetActive(false);
        }

        #endregion

        #region Interaction

        public override string GetInteractionHint()
        {
            if (_isInUse)
            {
                return $"[E] {DisplayName} (In Use)";
            }

            string typeName = _stationType switch
            {
                CraftStationType.Forge => "Forge",
                CraftStationType.Alchemy => "Alchemy Table",
                CraftStationType.Cooking => "Cooking Station",
                CraftStationType.Enchanting => "Enchanting Table",
                CraftStationType.Workbench => "Workbench",
                _ => "Craft Station"
            };

            return $"[E] Use {DisplayName}";
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            if (!base.CanInteract(ctx)) return false;

            // 已被其他人使用
            if (_isInUse && _currentInteractor != ctx.Interactor)
            {
                return false;
            }

            return true;
        }

        public override string GetCannotInteractReason(InteractionContext ctx)
        {
            if (_isInUse && _currentInteractor != ctx.Interactor)
            {
                return "Already in use";
            }

            return base.GetCannotInteractReason(ctx);
        }

        protected override InteractionResult ExecuteInteraction(InteractionContext ctx)
        {
            if (_isInUse)
            {
                // 关闭工作台
                CloseStation();
                return InteractionResult.Succeeded(this);
            }

            // 打开工作台
            return OpenStation(ctx);
        }

        #endregion

        #region Station Logic

        private InteractionResult OpenStation(InteractionContext ctx)
        {
            _isInUse = true;
            _currentInteractor = ctx.Interactor;

            // 播放音效
            PlaySound(_openSound);

            // 切换特效
            if (_idleVFX != null) _idleVFX.SetActive(false);
            if (_activeVFX != null) _activeVFX.SetActive(true);

            // 锁定玩家
            if (_lockPlayerMovement)
            {
                // TODO: 通过事件或接口锁定玩家移动
            }

            // 打开 UI
            OpenCraftUI();

            // 触发事件
            OnStationUsed?.Invoke(this, ctx.Interactor);

            return InteractionResult.Succeeded(this);
        }

        private void OpenCraftUI()
        {
            switch (_uiMode)
            {
                case CraftUIMode.OpenPanel:
                    // 通过 UIManager 打开制作面板
#if ZEROENGINE_UI
                    // UIManager.Instance.Open<CraftingView>(new { Station = this });
#endif
                    break;

                case CraftUIMode.SpawnPrefab:
                    if (_craftUIPrefab != null)
                    {
                        var ui = Instantiate(_craftUIPrefab);
                        // 设置工作台引用
                        var craftUI = ui.GetComponent<ICraftStationUI>();
                        craftUI?.SetStation(this);
                    }
                    break;

                case CraftUIMode.Event:
                    // 仅触发事件，由外部处理 UI
                    break;
            }
        }

        /// <summary>
        /// 关闭工作台
        /// </summary>
        public void CloseStation()
        {
            if (!_isInUse) return;

            _isInUse = false;
            _currentInteractor = null;

            // 播放音效
            PlaySound(_closeSound);

            // 切换特效
            if (_idleVFX != null) _idleVFX.SetActive(true);
            if (_activeVFX != null) _activeVFX.SetActive(false);

            // 解锁玩家
            if (_lockPlayerMovement)
            {
                // TODO: 解锁玩家移动
            }

            // 触发事件
            OnStationClosed?.Invoke(this);
        }

        #endregion

        #region Crafting API

        /// <summary>
        /// 开始制作
        /// </summary>
        public void StartCrafting(string recipeId)
        {
            if (!_isInUse) return;

            OnCraftStarted?.Invoke(this, recipeId);

            // 播放制作循环音效
            if (_craftingLoopSound != null && _audioSource != null)
            {
                _audioSource.clip = _craftingLoopSound;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            // 与 Crafting 系统集成
#if ZEROENGINE_CRAFTING
            var craftingManager = ZeroEngine.Crafting.CraftingManager.Instance;
            if (craftingManager != null)
            {
                craftingManager.StartCraft(recipeId);
            }
#endif
        }

        /// <summary>
        /// 完成制作
        /// </summary>
        public void CompleteCrafting(string recipeId, bool success)
        {
            // 停止循环音效
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            OnCraftCompleted?.Invoke(this, recipeId, success);
        }

        /// <summary>
        /// 检查是否可以制作指定配方
        /// </summary>
        public bool CanCraftRecipe(string recipeId)
        {
#if ZEROENGINE_CRAFTING
            var craftingManager = ZeroEngine.Crafting.CraftingManager.Instance;
            if (craftingManager != null)
            {
                // 检查配方是否在此工作台可用
                // 检查等级要求等
                return true;
            }
#endif
            return true;
        }

        /// <summary>
        /// 获取可用配方列表
        /// </summary>
        public string[] GetAvailableRecipes()
        {
#if ZEROENGINE_CRAFTING
            var craftingManager = ZeroEngine.Crafting.CraftingManager.Instance;
            if (craftingManager != null)
            {
                // 根据工作台类型和等级过滤配方
                return craftingManager.GetUnlockedRecipeIds();
            }
#endif
            return Array.Empty<string>();
        }

        #endregion

        #region Helpers

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 升级工作台
        /// </summary>
        public void Upgrade()
        {
            _stationLevel++;
        }

        /// <summary>
        /// 设置工作台等级
        /// </summary>
        public void SetLevel(int level)
        {
            _stationLevel = Mathf.Max(1, level);
        }

        #endregion
    }

    /// <summary>
    /// 工作台类型
    /// </summary>
    public enum CraftStationType
    {
        General,        // 通用
        Forge,          // 锻造
        Alchemy,        // 炼金
        Cooking,        // 烹饪
        Enchanting,     // 附魔
        Workbench,      // 工作台
        Custom          // 自定义
    }

    /// <summary>
    /// 制作 UI 打开模式
    /// </summary>
    public enum CraftUIMode
    {
        OpenPanel,      // 通过 UIManager 打开面板
        SpawnPrefab,    // 实例化预制体
        Event           // 仅触发事件
    }

    /// <summary>
    /// 工作台 UI 接口
    /// </summary>
    public interface ICraftStationUI
    {
        void SetStation(InteractableCraftStation station);
    }
}
