using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 教程进度指示器 (v1.14.0+)
    /// 显示当前教程进度
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialProgressView : MonoBehaviour
    {
        #region UI References

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _container;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _stepText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _progressFill;

        [Header("Step Indicators")]
        [SerializeField] private Transform _stepsContainer;
        [SerializeField] private GameObject _stepIndicatorPrefab;

        #endregion

        #region Runtime

        private TutorialUIManager _manager;
        private int _currentStep;
        private int _totalSteps;
        private readonly System.Collections.Generic.List<Image> _stepIndicators = new();

        #endregion

        #region Initialization

        public void Initialize(TutorialUIManager manager)
        {
            _manager = manager;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // 应用配置
            ApplyConfig();

            // 初始状态
            _canvasGroup.alpha = 0f;
        }

        private void ApplyConfig()
        {
            var config = _manager?.Config;
            if (config == null) return;

            // 设置位置
            SetPosition(config.ProgressPosition);

            // 设置颜色
            if (_progressFill != null)
            {
                _progressFill.color = config.ProgressColor;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置进度
        /// </summary>
        public void SetProgress(int current, int total)
        {
            _currentStep = current;
            _totalSteps = total;

            // 更新文本
            if (_stepText != null)
            {
                _stepText.text = $"{current}/{total}";
            }

            // 更新进度条
            float progress = total > 0 ? (float)current / total : 0f;
            SetProgress(progress);

            // 更新步骤指示器
            UpdateStepIndicators();

            // 淡入
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 设置进度 (0-1)
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_progressBar != null)
            {
                _progressBar.value = progress;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{progress * 100:F0}%";
            }
        }

        /// <summary>
        /// 设置标题文本
        /// </summary>
        public void SetTitle(string title)
        {
            if (_titleText != null)
            {
                _titleText.text = title ?? "";
                _titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
        }

        /// <summary>
        /// 设置描述文本
        /// </summary>
        public void SetText(string text)
        {
            if (_stepText != null)
            {
                _stepText.text = text ?? "";
            }
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        public void SetPosition(ProgressPosition position)
        {
            if (_container == null) return;

            switch (position)
            {
                case ProgressPosition.TopLeft:
                    SetAnchor(0, 1, 0, 1, new Vector2(20, -20));
                    break;

                case ProgressPosition.TopCenter:
                    SetAnchor(0.5f, 1, 0.5f, 1, new Vector2(0, -20));
                    break;

                case ProgressPosition.TopRight:
                    SetAnchor(1, 1, 1, 1, new Vector2(-20, -20));
                    break;

                case ProgressPosition.BottomLeft:
                    SetAnchor(0, 0, 0, 0, new Vector2(20, 20));
                    break;

                case ProgressPosition.BottomCenter:
                    SetAnchor(0.5f, 0, 0.5f, 0, new Vector2(0, 20));
                    break;

                case ProgressPosition.BottomRight:
                    SetAnchor(1, 0, 1, 0, new Vector2(-20, 20));
                    break;
            }
        }

        /// <summary>
        /// 隐藏进度指示器
        /// </summary>
        public void Hide()
        {
            StartCoroutine(FadeOutAndDeactivate());
        }

        #endregion

        #region Private Methods

        private void SetAnchor(float anchorX, float anchorY, float pivotX, float pivotY, Vector2 position)
        {
            _container.anchorMin = new Vector2(anchorX, anchorY);
            _container.anchorMax = new Vector2(anchorX, anchorY);
            _container.pivot = new Vector2(pivotX, pivotY);
            _container.anchoredPosition = position;
        }

        private void UpdateStepIndicators()
        {
            if (_stepsContainer == null || _stepIndicatorPrefab == null) return;

            // 如果步骤数量太多，不使用单独的指示器
            if (_totalSteps > 10)
            {
                ClearStepIndicators();
                return;
            }

            // 确保有足够的指示器
            while (_stepIndicators.Count < _totalSteps)
            {
                var go = Instantiate(_stepIndicatorPrefab, _stepsContainer);
                var image = go.GetComponent<Image>();
                if (image != null)
                {
                    _stepIndicators.Add(image);
                }
            }

            // 隐藏多余的指示器
            for (int i = 0; i < _stepIndicators.Count; i++)
            {
                if (i < _totalSteps)
                {
                    _stepIndicators[i].gameObject.SetActive(true);

                    // 设置颜色
                    var config = _manager?.Config;
                    Color activeColor = config?.ProgressColor ?? Color.green;
                    Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    _stepIndicators[i].color = i < _currentStep ? activeColor : inactiveColor;

                    // 当前步骤高亮
                    if (i == _currentStep - 1)
                    {
                        _stepIndicators[i].transform.localScale = Vector3.one * 1.2f;
                    }
                    else
                    {
                        _stepIndicators[i].transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    _stepIndicators[i].gameObject.SetActive(false);
                }
            }
        }

        private void ClearStepIndicators()
        {
            foreach (var indicator in _stepIndicators)
            {
                if (indicator != null)
                {
                    Destroy(indicator.gameObject);
                }
            }
            _stepIndicators.Clear();
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
            gameObject.SetActive(false);
        }

        #endregion

        #region Animation

        private void OnEnable()
        {
            // 可以添加进入动画
        }

        /// <summary>
        /// 显示步骤完成动画
        /// </summary>
        public void ShowStepComplete()
        {
            StartCoroutine(StepCompleteAnimation());
        }

        private IEnumerator StepCompleteAnimation()
        {
            // 缩放弹跳效果
            if (_container == null) yield break;

            Vector3 originalScale = _container.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // 弹跳曲线
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
                _container.localScale = originalScale * scale;

                yield return null;
            }

            _container.localScale = originalScale;
        }

        /// <summary>
        /// 显示教程完成动画
        /// </summary>
        public void ShowTutorialComplete()
        {
            StartCoroutine(TutorialCompleteAnimation());
        }

        private IEnumerator TutorialCompleteAnimation()
        {
            // 庆祝动画
            if (_progressFill != null)
            {
                var originalColor = _progressFill.color;
                float duration = 0.5f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / duration;

                    // 颜色闪烁
                    _progressFill.color = Color.Lerp(originalColor, Color.white, Mathf.PingPong(t * 4, 1));

                    yield return null;
                }

                _progressFill.color = originalColor;
            }

            // 播放完成音效
            _manager?.PlayTutorialCompleteSound();

            // 延迟后隐藏
            yield return new WaitForSecondsRealtime(1f);
            Hide();
        }

        #endregion

        private void OnDestroy()
        {
            ClearStepIndicators();
        }
    }
}
