using System;
using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 可拾取物品 (v1.14.0+)
    /// </summary>
    public class InteractableItem : InteractableBase
    {
        #region Serialized Fields

        [Header("Item Settings")]
        [SerializeField]
        [Tooltip("物品数据 (ScriptableObject)")]
        private ScriptableObject _itemData;

        [SerializeField]
        [Tooltip("物品 ID (如果不使用 ItemData)")]
        private string _itemId;

        [SerializeField]
        [Tooltip("拾取数量")]
        private int _amount = 1;

        [SerializeField]
        [Tooltip("拾取后销毁")]
        private bool _destroyOnPickup = true;

        [SerializeField]
        [Tooltip("拾取后禁用 (如果不销毁)")]
        private bool _disableOnPickup = true;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("拾取时播放的特效")]
        private GameObject _pickupVFX;

        [SerializeField]
        [Tooltip("悬浮动画")]
        private bool _enableFloatAnimation = false;

        [SerializeField]
        private float _floatHeight = 0.2f;

        [SerializeField]
        private float _floatSpeed = 2f;

        [SerializeField]
        private float _rotateSpeed = 30f;

        #endregion

        #region Private Fields

        private Vector3 _originalPosition;
        private bool _isPickedUp;

        #endregion

        #region Properties

        /// <summary>物品数据</summary>
        public ScriptableObject ItemData => _itemData;

        /// <summary>物品 ID</summary>
        public string ItemId => !string.IsNullOrEmpty(_itemId) ? _itemId : (_itemData != null ? _itemData.name : InteractableId);

        /// <summary>拾取数量</summary>
        public int Amount => _amount;

        /// <summary>是否已被拾取</summary>
        public bool IsPickedUp => _isPickedUp;

        #endregion

        #region Events

        /// <summary>物品被拾取时触发</summary>
        public event Action<InteractableItem, GameObject> OnItemPickedUp;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = InteractionType.Pickup;
            _originalPosition = transform.position;

            if (string.IsNullOrEmpty(_displayName) && _itemData != null)
            {
                _displayName = _itemData.name;
            }
        }

        private void Update()
        {
            if (_enableFloatAnimation && !_isPickedUp)
            {
                // 悬浮动画
                float yOffset = Mathf.Sin(Time.time * _floatSpeed) * _floatHeight;
                transform.position = _originalPosition + Vector3.up * yOffset;

                // 旋转
                transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Interaction

        protected override InteractionResult ExecuteInteraction(InteractionContext ctx)
        {
            if (_isPickedUp)
            {
                return InteractionResult.Failed(this, "Already picked up");
            }

            // 尝试添加到背包
            bool addedToInventory = TryAddToInventory(ctx.Interactor);

            if (!addedToInventory)
            {
                return InteractionResult.Failed(this, "Inventory full");
            }

            _isPickedUp = true;

            // 播放特效
            if (_pickupVFX != null)
            {
                Instantiate(_pickupVFX, transform.position, Quaternion.identity);
            }

            // 触发事件
            OnItemPickedUp?.Invoke(this, ctx.Interactor);

            // 处理物品对象
            if (_destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else if (_disableOnPickup)
            {
                gameObject.SetActive(false);
            }

            return InteractionResult.Succeeded(this, _itemData);
        }

        private bool TryAddToInventory(GameObject interactor)
        {
            // 与 Inventory 系统集成
#if ZEROENGINE_INVENTORY
            var inventory = ZeroEngine.Inventory.InventoryManager.Instance;
            if (inventory != null)
            {
                if (_itemData is ZeroEngine.Inventory.InventoryItemSO itemSO)
                {
                    return inventory.AddItem(itemSO, _amount);
                }
                else if (!string.IsNullOrEmpty(_itemId))
                {
                    return inventory.AddItemById(_itemId, _amount);
                }
            }
#endif
            // 无背包系统时默认成功
            return true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置物品数据
        /// </summary>
        public void SetItem(ScriptableObject itemData, int amount = 1)
        {
            _itemData = itemData;
            _amount = Mathf.Max(1, amount);

            if (itemData != null)
            {
                _displayName = itemData.name;
            }
        }

        /// <summary>
        /// 重置物品 (重新启用)
        /// </summary>
        public void ResetItem()
        {
            _isPickedUp = false;
            _isEnabled = true;
            transform.position = _originalPosition;
            gameObject.SetActive(true);
        }

        #endregion
    }
}
