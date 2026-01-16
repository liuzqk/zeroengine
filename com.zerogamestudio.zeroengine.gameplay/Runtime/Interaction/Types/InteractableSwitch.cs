using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 开关/机关 (v1.14.0+)
    /// </summary>
    public class InteractableSwitch : InteractableBase
    {
        #region Enums

        /// <summary>开关类型</summary>
        public enum SwitchType
        {
            Toggle,         // 切换型
            Momentary,      // 瞬时型 (自动复位)
            OneShot         // 一次性
        }

        #endregion

        #region Serialized Fields

        [Header("Switch Settings")]
        [SerializeField]
        [Tooltip("开关类型")]
        private SwitchType _switchType = SwitchType.Toggle;

        [SerializeField]
        [Tooltip("初始状态 (是否激活)")]
        private bool _isActivated = false;

        [SerializeField]
        [Tooltip("瞬时复位延迟 (秒)")]
        private float _momentaryResetDelay = 0.5f;

        [Header("Linked Objects")]
        [SerializeField]
        [Tooltip("联动对象列表")]
        private LinkedObject[] _linkedObjects;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("开关 Transform")]
        private Transform _switchTransform;

        [SerializeField]
        [Tooltip("激活时的旋转/位置变化")]
        private Vector3 _activatedOffset = Vector3.zero;

        [SerializeField]
        [Tooltip("使用旋转 (否则使用位置)")]
        private bool _useRotation = true;

        [SerializeField]
        [Tooltip("动画时间")]
        private float _animationDuration = 0.3f;

        [SerializeField]
        [Tooltip("使用 Animator")]
        private Animator _animator;

        [SerializeField]
        [Tooltip("激活动画参数")]
        private string _activatedAnimParam = "IsActivated";

        [Header("Visual")]
        [SerializeField]
        [Tooltip("激活时的材质/颜色")]
        private Material _activatedMaterial;

        [SerializeField]
        [Tooltip("目标 Renderer")]
        private Renderer _targetRenderer;

        [SerializeField]
        [Tooltip("激活时发光")]
        private bool _glowOnActivate = true;

        [SerializeField]
        [Tooltip("发光颜色")]
        private Color _glowColor = Color.green;

        [Header("Audio")]
        [SerializeField]
        private AudioClip _activateSound;

        [SerializeField]
        private AudioClip _deactivateSound;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onActivate;

        [SerializeField]
        private UnityEvent _onDeactivate;

        #endregion

        #region Private Fields

        private Coroutine _animationCoroutine;
        private Coroutine _resetCoroutine;
        private Vector3 _initialOffset;
        private Material _originalMaterial;

        #endregion

        #region Properties

        /// <summary>是否激活</summary>
        public bool IsActivated => _isActivated;

        /// <summary>开关类型</summary>
        public new SwitchType Type => _switchType;

        #endregion

        #region Events

        /// <summary>激活时触发</summary>
        public event Action<InteractableSwitch> OnSwitchActivated;

        /// <summary>取消激活时触发</summary>
        public event Action<InteractableSwitch> OnSwitchDeactivated;

        /// <summary>状态变更</summary>
        public event Action<InteractableSwitch, bool> OnStateChanged;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _interactionType = InteractionType.Activate;

            if (_switchTransform == null)
            {
                _switchTransform = transform;
            }

            _initialOffset = _useRotation
                ? _switchTransform.localEulerAngles
                : _switchTransform.localPosition;

            if (_targetRenderer != null)
            {
                _originalMaterial = _targetRenderer.material;
            }

            // 应用初始状态
            ApplyState(_isActivated, false);
        }

        #endregion

        #region Interaction

        public override string GetInteractionHint()
        {
            if (_switchType == SwitchType.OneShot && _isActivated)
            {
                return $"[E] {DisplayName} (Used)";
            }

            return _isActivated
                ? $"[E] Deactivate {DisplayName}"
                : $"[E] Activate {DisplayName}";
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            if (!base.CanInteract(ctx)) return false;

            // 一次性开关已使用
            if (_switchType == SwitchType.OneShot && _isActivated)
            {
                return false;
            }

            return true;
        }

        public override string GetCannotInteractReason(InteractionContext ctx)
        {
            if (_switchType == SwitchType.OneShot && _isActivated)
            {
                return "Already used";
            }

            return base.GetCannotInteractReason(ctx);
        }

        protected override InteractionResult ExecuteInteraction(InteractionContext ctx)
        {
            switch (_switchType)
            {
                case SwitchType.Toggle:
                    SetState(!_isActivated);
                    break;

                case SwitchType.Momentary:
                    SetState(true);
                    if (_resetCoroutine != null)
                    {
                        StopCoroutine(_resetCoroutine);
                    }
                    _resetCoroutine = StartCoroutine(MomentaryReset());
                    break;

                case SwitchType.OneShot:
                    SetState(true);
                    break;
            }

            return InteractionResult.Succeeded(this, _isActivated);
        }

        #endregion

        #region State Management

        /// <summary>
        /// 设置开关状态
        /// </summary>
        public void SetState(bool activated)
        {
            if (_isActivated == activated) return;

            _isActivated = activated;
            ApplyState(activated, true);

            // 触发事件
            OnStateChanged?.Invoke(this, activated);

            if (activated)
            {
                OnSwitchActivated?.Invoke(this);
                _onActivate?.Invoke();
                PlaySound(_activateSound);
            }
            else
            {
                OnSwitchDeactivated?.Invoke(this);
                _onDeactivate?.Invoke();
                PlaySound(_deactivateSound);
            }

            // 通知联动对象
            NotifyLinkedObjects(activated);
        }

        private void ApplyState(bool activated, bool animate)
        {
            // 动画
            if (_animator != null)
            {
                _animator.SetBool(_activatedAnimParam, activated);
            }
            else if (_switchTransform != null)
            {
                Vector3 targetOffset = activated ? _activatedOffset : Vector3.zero;

                if (animate && _animationDuration > 0)
                {
                    if (_animationCoroutine != null)
                    {
                        StopCoroutine(_animationCoroutine);
                    }
                    _animationCoroutine = StartCoroutine(AnimateSwitch(targetOffset));
                }
                else
                {
                    if (_useRotation)
                    {
                        _switchTransform.localEulerAngles = _initialOffset + targetOffset;
                    }
                    else
                    {
                        _switchTransform.localPosition = _initialOffset + targetOffset;
                    }
                }
            }

            // 材质变化
            if (_targetRenderer != null && _activatedMaterial != null)
            {
                _targetRenderer.material = activated ? _activatedMaterial : _originalMaterial;
            }

            // 发光效果
            if (_glowOnActivate && _targetRenderer != null)
            {
                var material = _targetRenderer.material;
                if (material.HasProperty("_EmissionColor"))
                {
                    if (activated)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", _glowColor);
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        private IEnumerator AnimateSwitch(Vector3 targetOffset)
        {
            float elapsed = 0;
            Vector3 start = _useRotation
                ? _switchTransform.localEulerAngles
                : _switchTransform.localPosition;
            Vector3 target = _initialOffset + targetOffset;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                t = t * t * (3f - 2f * t); // SmoothStep

                Vector3 current = Vector3.Lerp(start, target, t);

                if (_useRotation)
                {
                    _switchTransform.localEulerAngles = current;
                }
                else
                {
                    _switchTransform.localPosition = current;
                }

                yield return null;
            }
        }

        private IEnumerator MomentaryReset()
        {
            yield return new WaitForSeconds(_momentaryResetDelay);
            SetState(false);
            _resetCoroutine = null;
        }

        #endregion

        #region Linked Objects

        private void NotifyLinkedObjects(bool activated)
        {
            if (_linkedObjects == null) return;

            foreach (var linked in _linkedObjects)
            {
                if (linked.Target == null) continue;

                switch (linked.ActionType)
                {
                    case LinkedActionType.Toggle:
                        linked.Target.SetActive(activated);
                        break;

                    case LinkedActionType.Enable:
                        if (activated) linked.Target.SetActive(true);
                        break;

                    case LinkedActionType.Disable:
                        if (activated) linked.Target.SetActive(false);
                        break;

                    case LinkedActionType.SendMessage:
                        if (activated && !string.IsNullOrEmpty(linked.Message))
                        {
                            linked.Target.SendMessage(linked.Message, SendMessageOptions.DontRequireReceiver);
                        }
                        break;

                    case LinkedActionType.TriggerAnimation:
                        if (activated)
                        {
                            var anim = linked.Target.GetComponent<Animator>();
                            if (anim != null && !string.IsNullOrEmpty(linked.Message))
                            {
                                anim.SetTrigger(linked.Message);
                            }
                        }
                        break;
                }
            }
        }

        #endregion

        #region Helpers

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 激活开关
        /// </summary>
        public void Activate()
        {
            SetState(true);
        }

        /// <summary>
        /// 取消激活
        /// </summary>
        public void Deactivate()
        {
            SetState(false);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public void Toggle()
        {
            SetState(!_isActivated);
        }

        /// <summary>
        /// 重置开关 (包括一次性开关)
        /// </summary>
        public void Reset()
        {
            _isActivated = false;
            ApplyState(false, false);
        }

        /// <summary>
        /// 添加联动对象
        /// </summary>
        public void AddLinkedObject(GameObject target, LinkedActionType actionType, string message = null)
        {
            var list = _linkedObjects != null
                ? new List<LinkedObject>(_linkedObjects)
                : new List<LinkedObject>();

            list.Add(new LinkedObject
            {
                Target = target,
                ActionType = actionType,
                Message = message
            });

            _linkedObjects = list.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// 联动对象配置
    /// </summary>
    [Serializable]
    public struct LinkedObject
    {
        public GameObject Target;
        public LinkedActionType ActionType;
        public string Message;
    }

    /// <summary>
    /// 联动动作类型
    /// </summary>
    public enum LinkedActionType
    {
        Toggle,             // 切换激活状态
        Enable,             // 激活时启用
        Disable,            // 激活时禁用
        SendMessage,        // 发送消息
        TriggerAnimation    // 触发动画
    }
}
