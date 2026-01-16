using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 宝箱 (v1.14.0+)
    /// </summary>
    public class InteractableChest : InteractableBase
    {
        #region Serialized Fields

        [Header("Chest Settings")]
        [SerializeField]
        [Tooltip("是否锁定")]
        private bool _isLocked = false;

        [SerializeField]
        [Tooltip("钥匙物品 ID")]
        private string _keyItemId;

        [SerializeField]
        [Tooltip("使用钥匙后消耗")]
        private bool _consumeKey = true;

        [SerializeField]
        [Tooltip("打开后是否保持打开")]
        private bool _stayOpen = true;

        [SerializeField]
        [Tooltip("是否可重复开启")]
        private bool _reusable = false;

        [Header("Loot")]
        [SerializeField]
        [Tooltip("掉落表 (LootTableSO)")]
        private ScriptableObject _lootTable;

        [SerializeField]
        [Tooltip("固定物品列表 (如果不使用掉落表)")]
        private ChestLootItem[] _fixedLoot;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("盖子 Transform")]
        private Transform _lidTransform;

        [SerializeField]
        [Tooltip("打开角度")]
        private float _openAngle = -110f;

        [SerializeField]
        [Tooltip("动画时间")]
        private float _animationDuration = 0.5f;

        [SerializeField]
        [Tooltip("使用 Animator")]
        private Animator _animator;

        [SerializeField]
        [Tooltip("打开动画参数")]
        private string _openAnimParam = "Open";

        [Header("VFX")]
        [SerializeField]
        [Tooltip("打开时的特效")]
        private GameObject _openVFX;

        [SerializeField]
        [Tooltip("物品弹出特效")]
        private GameObject _lootPopVFX;

        [Header("Audio")]
        [SerializeField]
        private AudioClip _openSound;

        [SerializeField]
        private AudioClip _lockedSound;

        #endregion

        #region Private Fields

        private bool _isOpened = false;
        private Coroutine _animationCoroutine;
        private Vector3 _lidInitialRotation;

        #endregion

        #region Properties

        /// <summary>是否已打开</summary>
        public bool IsOpened => _isOpened;

        /// <summary>是否锁定</summary>
        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        #endregion

        #region Events

        /// <summary>宝箱打开</summary>
        public event Action<InteractableChest> OnChestOpened;

        /// <summary>获得掉落物</summary>
        public event Action<InteractableChest, List<ChestLootResult>> OnLootReceived;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = InteractionType.Open;

            if (_lidTransform != null)
            {
                _lidInitialRotation = _lidTransform.localEulerAngles;
            }
        }

        #endregion

        #region Interaction

        public override string GetInteractionHint()
        {
            if (_isOpened && !_reusable)
            {
                return $"[E] {DisplayName} (Empty)";
            }

            if (_isLocked)
            {
                return $"[E] {DisplayName} (Locked)";
            }

            return $"[E] Open {DisplayName}";
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            if (!base.CanInteract(ctx)) return false;

            if (_isOpened && !_reusable) return false;

            return true;
        }

        public override string GetCannotInteractReason(InteractionContext ctx)
        {
            if (_isOpened && !_reusable)
            {
                return "Already opened";
            }

            if (_isLocked && !HasKey(ctx.Interactor))
            {
                return string.IsNullOrEmpty(_keyItemId) ? "Locked" : $"Requires {_keyItemId}";
            }

            return base.GetCannotInteractReason(ctx);
        }

        protected override InteractionResult ExecuteInteraction(InteractionContext ctx)
        {
            // 处理锁定
            if (_isLocked)
            {
                if (!TryUnlock(ctx.Interactor))
                {
                    PlaySound(_lockedSound);
                    return InteractionResult.Failed(this, "Locked");
                }
                _isLocked = false;
            }

            // 打开宝箱
            return Open(ctx);
        }

        #endregion

        #region Open Logic

        private InteractionResult Open(InteractionContext ctx)
        {
            if (_isOpened && !_reusable)
            {
                return InteractionResult.Failed(this, "Already opened");
            }

            _isOpened = true;

            // 播放动画
            PlayOpenAnimation();

            // 播放音效
            PlaySound(_openSound);

            // 播放特效
            if (_openVFX != null)
            {
                Instantiate(_openVFX, transform.position + Vector3.up, Quaternion.identity);
            }

            // 发放掉落物
            var lootResults = DistributeLoot(ctx.Interactor);

            // 触发事件
            OnChestOpened?.Invoke(this);
            OnLootReceived?.Invoke(this, lootResults);

            // 如果可重用，延迟重置
            if (_reusable && !_stayOpen)
            {
                StartCoroutine(ResetAfterDelay(3f));
            }

            return InteractionResult.Succeeded(this, lootResults);
        }

        private void PlayOpenAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(_openAnimParam);
            }
            else if (_lidTransform != null)
            {
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
                _animationCoroutine = StartCoroutine(AnimateLid());
            }
        }

        private IEnumerator AnimateLid()
        {
            float elapsed = 0;
            Vector3 startRot = _lidTransform.localEulerAngles;
            Vector3 targetRot = _lidInitialRotation + Vector3.right * _openAngle;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                t = t * t * (3f - 2f * t); // SmoothStep

                _lidTransform.localEulerAngles = Vector3.Lerp(startRot, targetRot, t);
                yield return null;
            }

            _lidTransform.localEulerAngles = targetRot;
        }

        private List<ChestLootResult> DistributeLoot(GameObject receiver)
        {
            var results = new List<ChestLootResult>();

            // 使用掉落表
            if (_lootTable != null)
            {
#if ZEROENGINE_LOOT
                var lootManager = ZeroEngine.Loot.LootTableManager.Instance;
                if (lootManager != null)
                {
                    var lootResults = lootManager.Roll(_lootTable as ZeroEngine.Loot.LootTableSO);
                    foreach (var result in lootResults)
                    {
                        results.Add(new ChestLootResult
                        {
                            ItemId = result.Item?.name,
                            Amount = result.Amount,
                            ItemData = result.Item
                        });

                        // 添加到背包
                        AddToInventory(result.Item, result.Amount);
                    }
                }
#endif
            }
            else if (_fixedLoot != null && _fixedLoot.Length > 0)
            {
                // 使用固定掉落
                foreach (var loot in _fixedLoot)
                {
                    results.Add(new ChestLootResult
                    {
                        ItemId = loot.ItemId,
                        Amount = loot.Amount,
                        ItemData = loot.ItemData
                    });

                    AddToInventory(loot.ItemData, loot.Amount);
                }
            }

            // 播放物品弹出特效
            if (_lootPopVFX != null && results.Count > 0)
            {
                Instantiate(_lootPopVFX, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            return results;
        }

        private void AddToInventory(ScriptableObject itemData, int amount)
        {
#if ZEROENGINE_INVENTORY
            var inventory = ZeroEngine.Inventory.InventoryManager.Instance;
            if (inventory != null && itemData is ZeroEngine.Inventory.InventoryItemSO itemSO)
            {
                inventory.AddItem(itemSO, amount);
            }
#endif
        }

        private IEnumerator ResetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Reset();
        }

        #endregion

        #region Helpers

        private bool HasKey(GameObject interactor)
        {
            if (string.IsNullOrEmpty(_keyItemId)) return false;

#if ZEROENGINE_INVENTORY
            var inventory = ZeroEngine.Inventory.InventoryManager.Instance;
            return inventory != null && inventory.HasItem(_keyItemId);
#else
            return false;
#endif
        }

        private bool TryUnlock(GameObject interactor)
        {
            if (string.IsNullOrEmpty(_keyItemId)) return true;

#if ZEROENGINE_INVENTORY
            var inventory = ZeroEngine.Inventory.InventoryManager.Instance;
            if (inventory != null && inventory.HasItem(_keyItemId))
            {
                if (_consumeKey)
                {
                    inventory.RemoveItem(_keyItemId, 1);
                }
                return true;
            }
#endif
            return false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 重置宝箱
        /// </summary>
        public void Reset()
        {
            _isOpened = false;

            if (_lidTransform != null)
            {
                _lidTransform.localEulerAngles = _lidInitialRotation;
            }

            if (_animator != null)
            {
                _animator.Rebind();
            }
        }

        /// <summary>
        /// 锁定宝箱
        /// </summary>
        public void Lock(string keyItemId = null)
        {
            _isLocked = true;
            if (!string.IsNullOrEmpty(keyItemId))
            {
                _keyItemId = keyItemId;
            }
        }

        /// <summary>
        /// 解锁宝箱
        /// </summary>
        public void Unlock()
        {
            _isLocked = false;
        }

        /// <summary>
        /// 设置掉落物
        /// </summary>
        public void SetLoot(ChestLootItem[] loot)
        {
            _fixedLoot = loot;
        }

        #endregion
    }

    /// <summary>
    /// 宝箱掉落物配置
    /// </summary>
    [Serializable]
    public struct ChestLootItem
    {
        public string ItemId;
        public ScriptableObject ItemData;
        public int Amount;
    }

    /// <summary>
    /// 宝箱掉落结果
    /// </summary>
    public struct ChestLootResult
    {
        public string ItemId;
        public int Amount;
        public ScriptableObject ItemData;
    }
}
