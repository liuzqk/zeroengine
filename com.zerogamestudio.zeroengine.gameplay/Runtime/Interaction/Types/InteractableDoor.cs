using System;
using System.Collections;
using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 门/传送点 (v1.14.0+)
    /// </summary>
    public class InteractableDoor : InteractableBase
    {
        #region Enums

        /// <summary>门类型</summary>
        public enum DoorType
        {
            Standard,       // 普通门 (开/关)
            Teleport,       // 传送门
            SceneTransition // 场景切换
        }

        /// <summary>门状态</summary>
        public enum DoorState
        {
            Closed,
            Opening,
            Open,
            Closing
        }

        #endregion

        #region Serialized Fields

        [Header("Door Settings")]
        [SerializeField]
        [Tooltip("门类型")]
        private DoorType _doorType = DoorType.Standard;

        [SerializeField]
        [Tooltip("是否自动关闭")]
        private bool _autoClose = true;

        [SerializeField]
        [Tooltip("自动关闭延迟 (秒)")]
        private float _autoCloseDelay = 3f;

        [SerializeField]
        [Tooltip("是否锁定")]
        private bool _isLocked = false;

        [SerializeField]
        [Tooltip("钥匙物品 ID")]
        private string _keyItemId;

        [SerializeField]
        [Tooltip("使用钥匙后消耗")]
        private bool _consumeKey = true;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("门 Transform (用于旋转/移动)")]
        private Transform _doorTransform;

        [SerializeField]
        [Tooltip("开门动画类型")]
        private DoorAnimationType _animationType = DoorAnimationType.Rotate;

        [SerializeField]
        [Tooltip("开门角度/距离")]
        private float _openAmount = 90f;

        [SerializeField]
        [Tooltip("动画时间")]
        private float _animationDuration = 0.5f;

        [SerializeField]
        [Tooltip("使用 Animator")]
        private Animator _animator;

        [SerializeField]
        [Tooltip("开门动画参数")]
        private string _openAnimParam = "IsOpen";

        [Header("Teleport Settings")]
        [SerializeField]
        [Tooltip("传送目标位置")]
        private Transform _teleportTarget;

        [SerializeField]
        [Tooltip("传送时保持朝向")]
        private bool _keepRotation = false;

        [Header("Scene Transition")]
        [SerializeField]
        [Tooltip("目标场景名称")]
        private string _targetSceneName;

        [SerializeField]
        [Tooltip("目标位置标识 (SpawnPoint ID)")]
        private string _spawnPointId;

        [Header("Audio")]
        [SerializeField]
        private AudioClip _openSound;

        [SerializeField]
        private AudioClip _closeSound;

        [SerializeField]
        private AudioClip _lockedSound;

        #endregion

        #region Private Fields

        private DoorState _state = DoorState.Closed;
        private Coroutine _animationCoroutine;
        private Coroutine _autoCloseCoroutine;
        private Vector3 _initialRotation;
        private Vector3 _initialPosition;
        private AudioSource _audioSource;

        #endregion

        #region Properties

        /// <summary>门类型</summary>
        public new DoorType Type => _doorType;

        /// <summary>当前状态</summary>
        public DoorState State => _state;

        /// <summary>是否打开</summary>
        public bool IsOpen => _state == DoorState.Open;

        /// <summary>是否锁定</summary>
        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        #endregion

        #region Events

        /// <summary>门打开</summary>
        public event Action<InteractableDoor> OnDoorOpened;

        /// <summary>门关闭</summary>
        public event Action<InteractableDoor> OnDoorClosed;

        /// <summary>传送完成</summary>
        public event Action<InteractableDoor, GameObject> OnTeleported;

        /// <summary>场景切换</summary>
        public event Action<InteractableDoor, string> OnSceneTransition;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = _doorType == DoorType.Teleport || _doorType == DoorType.SceneTransition
                ? InteractionType.Enter
                : InteractionType.Open;

            if (_doorTransform == null)
            {
                _doorTransform = transform;
            }

            _initialRotation = _doorTransform.localEulerAngles;
            _initialPosition = _doorTransform.localPosition;
            _audioSource = GetComponent<AudioSource>();
        }

        #endregion

        #region Interaction

        public override string GetInteractionHint()
        {
            if (_isLocked)
            {
                return $"[E] {DisplayName} (Locked)";
            }

            return _doorType switch
            {
                DoorType.Standard => IsOpen ? $"[E] Close {DisplayName}" : $"[E] Open {DisplayName}",
                DoorType.Teleport => $"[E] Enter {DisplayName}",
                DoorType.SceneTransition => $"[E] Enter {DisplayName}",
                _ => base.GetInteractionHint()
            };
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            if (!base.CanInteract(ctx)) return false;

            // 检查锁定状态
            if (_isLocked && !HasKey(ctx.Interactor))
            {
                return false;
            }

            return true;
        }

        public override string GetCannotInteractReason(InteractionContext ctx)
        {
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
                if (TryUnlock(ctx.Interactor))
                {
                    _isLocked = false;
                }
                else
                {
                    PlaySound(_lockedSound);
                    return InteractionResult.Failed(this, "Locked");
                }
            }

            return _doorType switch
            {
                DoorType.Standard => HandleStandardDoor(ctx),
                DoorType.Teleport => HandleTeleport(ctx),
                DoorType.SceneTransition => HandleSceneTransition(ctx),
                _ => InteractionResult.Failed(this, "Unknown door type")
            };
        }

        #endregion

        #region Door Logic

        private InteractionResult HandleStandardDoor(InteractionContext ctx)
        {
            if (_state == DoorState.Opening || _state == DoorState.Closing)
            {
                return InteractionResult.Failed(this, "Door is moving");
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }

            return InteractionResult.Succeeded(this);
        }

        private InteractionResult HandleTeleport(InteractionContext ctx)
        {
            if (_teleportTarget == null)
            {
                return InteractionResult.Failed(this, "No teleport target");
            }

            var interactor = ctx.Interactor;
            if (interactor != null)
            {
                interactor.transform.position = _teleportTarget.position;
                if (!_keepRotation)
                {
                    interactor.transform.rotation = _teleportTarget.rotation;
                }

                OnTeleported?.Invoke(this, interactor);
            }

            return InteractionResult.Succeeded(this, _teleportTarget);
        }

        private InteractionResult HandleSceneTransition(InteractionContext ctx)
        {
            if (string.IsNullOrEmpty(_targetSceneName))
            {
                return InteractionResult.Failed(this, "No target scene");
            }

            OnSceneTransition?.Invoke(this, _targetSceneName);

            // 场景加载
            UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);

            return InteractionResult.Succeeded(this, _targetSceneName);
        }

        #endregion

        #region Animation

        public void Open()
        {
            if (_state == DoorState.Open || _state == DoorState.Opening) return;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            if (_animator != null)
            {
                _animator.SetBool(_openAnimParam, true);
                _state = DoorState.Open;
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateDoor(true));
            }

            PlaySound(_openSound);

            // 自动关闭
            if (_autoClose)
            {
                if (_autoCloseCoroutine != null)
                {
                    StopCoroutine(_autoCloseCoroutine);
                }
                _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
            }
        }

        public void Close()
        {
            if (_state == DoorState.Closed || _state == DoorState.Closing) return;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }

            if (_animator != null)
            {
                _animator.SetBool(_openAnimParam, false);
                _state = DoorState.Closed;
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateDoor(false));
            }

            PlaySound(_closeSound);
        }

        private IEnumerator AnimateDoor(bool open)
        {
            _state = open ? DoorState.Opening : DoorState.Closing;

            float elapsed = 0;
            Vector3 startRot = _doorTransform.localEulerAngles;
            Vector3 startPos = _doorTransform.localPosition;

            Vector3 targetRot = open
                ? _initialRotation + Vector3.up * _openAmount
                : _initialRotation;

            Vector3 targetPos = open
                ? _initialPosition + Vector3.right * _openAmount
                : _initialPosition;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                t = t * t * (3f - 2f * t); // SmoothStep

                if (_animationType == DoorAnimationType.Rotate)
                {
                    _doorTransform.localEulerAngles = Vector3.Lerp(startRot, targetRot, t);
                }
                else
                {
                    _doorTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                }

                yield return null;
            }

            _state = open ? DoorState.Open : DoorState.Closed;

            if (open)
            {
                OnDoorOpened?.Invoke(this);
            }
            else
            {
                OnDoorClosed?.Invoke(this);
            }
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(_autoCloseDelay);
            Close();
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
        /// 锁定门
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
        /// 解锁门
        /// </summary>
        public void Unlock()
        {
            _isLocked = false;
        }

        /// <summary>
        /// 设置传送目标
        /// </summary>
        public void SetTeleportTarget(Transform target)
        {
            _teleportTarget = target;
        }

        #endregion
    }

    /// <summary>
    /// 门动画类型
    /// </summary>
    public enum DoorAnimationType
    {
        Rotate,     // 旋转
        Slide       // 滑动
    }
}
