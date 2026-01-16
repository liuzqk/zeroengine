using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 教程箭头指示器 (v1.14.0+)
    /// 用于指示世界空间目标位置
    /// 支持屏幕内 3D 箭头和屏幕边缘指示
    /// </summary>
    public class TutorialArrowUI : MonoBehaviour
    {
        #region Settings

        [Header("World Space Arrow")]
        [SerializeField] private Transform _worldArrow;
        [SerializeField] private SpriteRenderer _worldArrowRenderer;

        [Header("Screen Space Arrow")]
        [SerializeField] private RectTransform _screenArrow;
        [SerializeField] private Image _screenArrowImage;
        [SerializeField] private Canvas _screenCanvas;

        [Header("Distance Indicator")]
        [SerializeField] private Text _distanceText;
        [SerializeField] private bool _showDistance = true;

        [Header("Animation")]
        [SerializeField] private float _bounceSpeed = 3f;
        [SerializeField] private float _bounceAmount = 0.5f;
        [SerializeField] private float _rotateSpeed = 0f;

        #endregion

        #region Runtime

        private TutorialUIManager _manager;
        private Vector3 _targetPosition;
        private Transform _followTarget;
        private ArrowMode _mode = ArrowMode.WorldSpace;
        private Camera _mainCamera;
        private Vector3 _initialScale;
        private Coroutine _animationCoroutine;

        #endregion

        #region Initialization

        public void Initialize(TutorialUIManager manager)
        {
            _manager = manager;
            _mainCamera = Camera.main;

            if (_worldArrow != null)
            {
                _initialScale = _worldArrow.localScale;
            }

            // 应用配置
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            var config = _manager?.Config;
            if (config == null) return;

            _bounceSpeed = config.ArrowBounceSpeed;
            _bounceAmount = config.ArrowBounceAmount / 10f; // 转换为合适的单位

            // 应用颜色
            if (_worldArrowRenderer != null)
            {
                _worldArrowRenderer.color = config.ArrowColor;
            }

            if (_screenArrowImage != null)
            {
                _screenArrowImage.color = config.ArrowColor;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置目标位置
        /// </summary>
        public void SetTarget(Vector3 worldPosition)
        {
            _targetPosition = worldPosition;
            _followTarget = null;
            UpdateArrowPosition();
        }

        /// <summary>
        /// 设置跟随目标
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            if (target != null)
            {
                _targetPosition = target.position;
            }
            UpdateArrowPosition();
        }

        /// <summary>
        /// 设置箭头模式
        /// </summary>
        public void SetMode(ArrowMode mode)
        {
            _mode = mode;
            UpdateArrowVisibility();
        }

        /// <summary>
        /// 设置箭头颜色
        /// </summary>
        public void SetColor(Color color)
        {
            if (_worldArrowRenderer != null)
            {
                _worldArrowRenderer.color = color;
            }

            if (_screenArrowImage != null)
            {
                _screenArrowImage.color = color;
            }
        }

        #endregion

        #region Update Loop

        private void OnEnable()
        {
            _mainCamera = Camera.main;
            StartAnimation();
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // 更新跟随目标位置
            if (_followTarget != null)
            {
                _targetPosition = _followTarget.position;
            }

            UpdateArrowPosition();
            UpdateDistanceText();
        }

        private void UpdateArrowPosition()
        {
            switch (_mode)
            {
                case ArrowMode.WorldSpace:
                    UpdateWorldSpaceArrow();
                    break;

                case ArrowMode.ScreenEdge:
                    UpdateScreenEdgeArrow();
                    break;

                case ArrowMode.Hybrid:
                    // 根据目标是否在屏幕内自动切换
                    if (IsTargetOnScreen())
                    {
                        UpdateWorldSpaceArrow();
                    }
                    else
                    {
                        UpdateScreenEdgeArrow();
                    }
                    break;
            }
        }

        private void UpdateWorldSpaceArrow()
        {
            if (_worldArrow == null) return;

            // 显示世界空间箭头
            _worldArrow.gameObject.SetActive(true);
            if (_screenArrow != null) _screenArrow.gameObject.SetActive(false);

            // 设置位置（在目标上方）
            Vector3 arrowPos = _targetPosition + Vector3.up * 2f;
            _worldArrow.position = arrowPos;

            // 面向相机
            if (_mainCamera != null)
            {
                Vector3 dirToCamera = _mainCamera.transform.position - _worldArrow.position;
                dirToCamera.y = 0;
                if (dirToCamera != Vector3.zero)
                {
                    _worldArrow.rotation = Quaternion.LookRotation(dirToCamera) * Quaternion.Euler(0, 180, 0);
                }
            }
        }

        private void UpdateScreenEdgeArrow()
        {
            if (_screenArrow == null || _mainCamera == null) return;

            // 显示屏幕空间箭头
            if (_worldArrow != null) _worldArrow.gameObject.SetActive(false);
            _screenArrow.gameObject.SetActive(true);

            // 计算目标在屏幕上的位置
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_targetPosition);
            bool isBehind = screenPos.z < 0;

            if (isBehind)
            {
                screenPos *= -1;
            }

            // 限制在屏幕边缘
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 direction = ((Vector2)screenPos - screenCenter).normalized;

            // 计算边缘位置
            float edgeMargin = 50f;
            float maxX = Screen.width - edgeMargin;
            float maxY = Screen.height - edgeMargin;
            float minX = edgeMargin;
            float minY = edgeMargin;

            Vector2 edgePos;
            if (!IsTargetOnScreen())
            {
                // 计算与屏幕边缘的交点
                edgePos = GetScreenEdgePosition(screenCenter, direction, minX, minY, maxX, maxY);
            }
            else
            {
                edgePos = screenPos;
            }

            // 转换为 Canvas 坐标
            if (_screenCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _screenCanvas.GetComponent<RectTransform>(),
                    edgePos,
                    _screenCanvas.worldCamera,
                    out Vector2 localPos);

                _screenArrow.anchoredPosition = localPos;
            }

            // 旋转箭头指向目标方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _screenArrow.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        private Vector2 GetScreenEdgePosition(Vector2 center, Vector2 direction, float minX, float minY, float maxX, float maxY)
        {
            // 计算与各边的交点
            float tLeft = (minX - center.x) / direction.x;
            float tRight = (maxX - center.x) / direction.x;
            float tBottom = (minY - center.y) / direction.y;
            float tTop = (maxY - center.y) / direction.y;

            float tMin = float.MaxValue;

            if (tLeft > 0 && tLeft < tMin)
            {
                Vector2 p = center + direction * tLeft;
                if (p.y >= minY && p.y <= maxY) tMin = tLeft;
            }
            if (tRight > 0 && tRight < tMin)
            {
                Vector2 p = center + direction * tRight;
                if (p.y >= minY && p.y <= maxY) tMin = tRight;
            }
            if (tBottom > 0 && tBottom < tMin)
            {
                Vector2 p = center + direction * tBottom;
                if (p.x >= minX && p.x <= maxX) tMin = tBottom;
            }
            if (tTop > 0 && tTop < tMin)
            {
                Vector2 p = center + direction * tTop;
                if (p.x >= minX && p.x <= maxX) tMin = tTop;
            }

            return center + direction * tMin;
        }

        private void UpdateArrowVisibility()
        {
            bool showWorld = _mode == ArrowMode.WorldSpace;
            bool showScreen = _mode == ArrowMode.ScreenEdge;

            if (_worldArrow != null)
            {
                _worldArrow.gameObject.SetActive(showWorld);
            }

            if (_screenArrow != null)
            {
                _screenArrow.gameObject.SetActive(showScreen);
            }
        }

        private bool IsTargetOnScreen()
        {
            if (_mainCamera == null) return false;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_targetPosition);
            return screenPos.z > 0 &&
                   screenPos.x > 0 && screenPos.x < Screen.width &&
                   screenPos.y > 0 && screenPos.y < Screen.height;
        }

        private void UpdateDistanceText()
        {
            if (_distanceText == null || !_showDistance) return;

            if (_mainCamera == null)
            {
                _distanceText.text = "";
                return;
            }

            float distance = Vector3.Distance(_mainCamera.transform.position, _targetPosition);
            _distanceText.text = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000f:F1}km";
        }

        #endregion

        #region Animation

        private void StartAnimation()
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimationCoroutine());
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private IEnumerator AnimationCoroutine()
        {
            float time = 0f;

            while (true)
            {
                time += Time.deltaTime;

                // 弹跳动画
                float bounceOffset = Mathf.Sin(time * _bounceSpeed * Mathf.PI) * _bounceAmount;

                if (_worldArrow != null && _worldArrow.gameObject.activeSelf)
                {
                    _worldArrow.localPosition = new Vector3(
                        _worldArrow.localPosition.x,
                        bounceOffset,
                        _worldArrow.localPosition.z
                    );

                    // 缩放脉冲
                    float scale = 1f + Mathf.Sin(time * _bounceSpeed * Mathf.PI * 0.5f) * 0.1f;
                    _worldArrow.localScale = _initialScale * scale;

                    // 旋转
                    if (_rotateSpeed > 0)
                    {
                        _worldArrow.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);
                    }
                }

                if (_screenArrow != null && _screenArrow.gameObject.activeSelf)
                {
                    // 屏幕箭头缩放脉冲
                    float scale = 1f + Mathf.Sin(time * _bounceSpeed * Mathf.PI) * 0.1f;
                    _screenArrow.localScale = Vector3.one * scale;
                }

                yield return null;
            }
        }

        #endregion

        #region Enums

        public enum ArrowMode
        {
            /// <summary>世界空间 3D 箭头</summary>
            WorldSpace,

            /// <summary>屏幕边缘 2D 箭头</summary>
            ScreenEdge,

            /// <summary>混合模式 (屏幕内显示 3D，屏幕外显示边缘)</summary>
            Hybrid
        }

        #endregion
    }
}
