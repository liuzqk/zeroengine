using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 教程高亮遮罩视图 (v1.14.0+)
    /// 用于高亮显示 UI 元素
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialHighlightView : MonoBehaviour, IPointerClickHandler
    {
        #region UI References

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _maskImage;
        [SerializeField] private RectTransform _highlightFrame;
        [SerializeField] private Image _highlightBorder;
        [SerializeField] private Text _hintText;
        [SerializeField] private RectTransform _fingerPointer;
        [SerializeField] private RectTransform _arrowPointer;

        [Header("Mask Material")]
        [SerializeField] private Material _maskMaterial;

        #endregion

        #region Runtime

        private TutorialUIManager _manager;
        private RectTransform _currentTarget;
        private HighlightType _currentType;
        private bool _waitForClick;
        private Coroutine _pulseCoroutine;
        private Coroutine _updateCoroutine;

        // Shader property IDs
        private static readonly int _CenterProp = Shader.PropertyToID("_Center");
        private static readonly int _RadiusProp = Shader.PropertyToID("_Radius");
        private static readonly int _SizeProp = Shader.PropertyToID("_Size");
        private static readonly int _FeatherProp = Shader.PropertyToID("_Feather");

        #endregion

        #region Initialization

        public void Initialize(TutorialUIManager manager)
        {
            _manager = manager;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // 初始状态
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 显示高亮
        /// </summary>
        public void ShowHighlight(RectTransform target, HighlightType type, string hintText = null)
        {
            if (target == null)
            {
                Debug.LogWarning("[Tutorial] Highlight target is null");
                return;
            }

            gameObject.SetActive(true);
            _currentTarget = target;
            _currentType = type;

            // 设置提示文本
            if (_hintText != null)
            {
                _hintText.text = hintText ?? "";
                _hintText.gameObject.SetActive(!string.IsNullOrEmpty(hintText));
            }

            // 根据类型设置高亮样式
            SetupHighlightType(type);

            // 更新位置
            UpdateHighlightPosition();

            // 开始持续更新位置（目标可能移动）
            StopUpdateCoroutine();
            _updateCoroutine = StartCoroutine(UpdatePositionCoroutine());

            // 开始脉冲动画
            StartPulseAnimation();

            // 淡入
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 隐藏高亮
        /// </summary>
        public void Hide()
        {
            StopPulseAnimation();
            StopUpdateCoroutine();
            StartCoroutine(FadeOutAndDeactivate());
        }

        /// <summary>
        /// 设置是否等待点击
        /// </summary>
        public void SetWaitForClick(bool wait)
        {
            _waitForClick = wait;
            _canvasGroup.blocksRaycasts = wait;
        }

        #endregion

        #region Private Methods

        private void SetupHighlightType(HighlightType type)
        {
            // 隐藏所有指示器
            if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);
            if (_arrowPointer != null) _arrowPointer.gameObject.SetActive(false);
            if (_highlightFrame != null) _highlightFrame.gameObject.SetActive(false);

            switch (type)
            {
                case HighlightType.Circle:
                    SetupCircleMask();
                    break;

                case HighlightType.Rectangle:
                    SetupRectangleMask();
                    if (_highlightFrame != null) _highlightFrame.gameObject.SetActive(true);
                    break;

                case HighlightType.Finger:
                    SetupRectangleMask();
                    if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(true);
                    break;

                case HighlightType.Arrow:
                    SetupRectangleMask();
                    if (_arrowPointer != null) _arrowPointer.gameObject.SetActive(true);
                    break;

                case HighlightType.Pulse:
                    SetupRectangleMask();
                    if (_highlightFrame != null) _highlightFrame.gameObject.SetActive(true);
                    break;
            }

            // 应用配置颜色
            var config = _manager?.Config;
            if (config != null)
            {
                if (_maskImage != null)
                {
                    _maskImage.color = config.MaskColor;
                }

                if (_highlightBorder != null)
                {
                    _highlightBorder.color = config.HighlightColor;
                }
            }
        }

        private void SetupCircleMask()
        {
            if (_maskMaterial == null || _maskImage == null) return;

            // 使用 shader 实现圆形遮罩
            var mat = new Material(_maskMaterial);
            _maskImage.material = mat;
        }

        private void SetupRectangleMask()
        {
            if (_maskImage == null) return;

            // 使用普通材质或 shader 实现矩形遮罩
            _maskImage.material = _maskMaterial;
        }

        private void UpdateHighlightPosition()
        {
            if (_currentTarget == null) return;

            var config = _manager?.Config;
            float padding = config?.HighlightPadding ?? 10f;

            // 获取目标的屏幕位置和大小
            var targetCorners = new Vector3[4];
            _currentTarget.GetWorldCorners(targetCorners);

            // 转换为本地坐标
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 minLocal, maxLocal;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetCorners[0]),
                canvas.worldCamera,
                out minLocal);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetCorners[2]),
                canvas.worldCamera,
                out maxLocal);

            // 计算中心和大小
            Vector2 center = (minLocal + maxLocal) / 2f;
            Vector2 size = new Vector2(
                Mathf.Abs(maxLocal.x - minLocal.x) + padding * 2,
                Mathf.Abs(maxLocal.y - minLocal.y) + padding * 2
            );

            // 更新高亮框位置
            if (_highlightFrame != null)
            {
                _highlightFrame.anchoredPosition = center;
                _highlightFrame.sizeDelta = size;
            }

            // 更新遮罩材质参数
            UpdateMaskMaterial(center, size);

            // 更新指示器位置
            UpdatePointerPosition(center, size);

            // 更新提示文本位置
            UpdateHintPosition(center, size);
        }

        private void UpdateMaskMaterial(Vector2 center, Vector2 size)
        {
            if (_maskImage?.material == null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;

            // 归一化坐标
            Vector2 normalizedCenter = new Vector2(
                (center.x / canvasSize.x) + 0.5f,
                (center.y / canvasSize.y) + 0.5f
            );

            Vector2 normalizedSize = new Vector2(
                size.x / canvasSize.x,
                size.y / canvasSize.y
            );

            var mat = _maskImage.material;
            mat.SetVector(_CenterProp, normalizedCenter);

            if (_currentType == HighlightType.Circle)
            {
                float radius = Mathf.Max(normalizedSize.x, normalizedSize.y) / 2f;
                mat.SetFloat(_RadiusProp, radius);
            }
            else
            {
                mat.SetVector(_SizeProp, normalizedSize / 2f);
            }

            mat.SetFloat(_FeatherProp, 0.01f);
        }

        private void UpdatePointerPosition(Vector2 center, Vector2 size)
        {
            // 手指指示器
            if (_fingerPointer != null && _fingerPointer.gameObject.activeSelf)
            {
                // 放在目标右下角
                _fingerPointer.anchoredPosition = center + new Vector2(size.x / 2f, -size.y / 2f);
            }

            // 箭头指示器
            if (_arrowPointer != null && _arrowPointer.gameObject.activeSelf)
            {
                // 放在目标上方
                _arrowPointer.anchoredPosition = center + new Vector2(0, size.y / 2f + 30f);
            }
        }

        private void UpdateHintPosition(Vector2 center, Vector2 size)
        {
            if (_hintText == null) return;

            var hintRect = _hintText.GetComponent<RectTransform>();
            if (hintRect == null) return;

            // 放在高亮区域下方
            hintRect.anchoredPosition = center + new Vector2(0, -size.y / 2f - 50f);
        }

        private IEnumerator UpdatePositionCoroutine()
        {
            while (true)
            {
                UpdateHighlightPosition();
                yield return null;
            }
        }

        private void StopUpdateCoroutine()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }

        private void StartPulseAnimation()
        {
            StopPulseAnimation();
            _pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private void StopPulseAnimation()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }

        private IEnumerator PulseCoroutine()
        {
            var config = _manager?.Config;
            float speed = config?.HighlightPulseSpeed ?? 2f;
            float amount = config?.HighlightPulseAmount ?? 0.1f;

            while (true)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * speed * Mathf.PI) * amount;

                if (_highlightFrame != null)
                {
                    _highlightFrame.localScale = Vector3.one * scale;
                }

                if (_highlightBorder != null)
                {
                    // 脉冲颜色透明度
                    var color = _highlightBorder.color;
                    color.a = 0.5f + Mathf.Sin(Time.unscaledTime * speed * Mathf.PI) * 0.5f;
                    _highlightBorder.color = color;
                }

                yield return null;
            }
        }

        private IEnumerator FadeIn()
        {
            float duration = _manager?.Config?.UIFadeInDuration ?? 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndDeactivate()
        {
            float duration = _manager?.Config?.UIFadeOutDuration ?? 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _currentTarget = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_waitForClick) return;

            // 检查是否点击在高亮区域内
            if (_currentTarget != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    _currentTarget,
                    eventData.position,
                    eventData.pressEventCamera))
                {
                    _manager?.PlayConfirmSound();
                    _manager?.TriggerHighlightTargetClicked();
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            StopPulseAnimation();
            StopUpdateCoroutine();

            // 清理动态创建的材质
            if (_maskImage?.material != null && _maskImage.material != _maskMaterial)
            {
                Destroy(_maskImage.material);
            }
        }
    }
}
