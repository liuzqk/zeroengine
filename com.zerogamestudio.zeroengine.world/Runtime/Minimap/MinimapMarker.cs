using UnityEngine;

namespace ZeroEngine.Minimap
{
    /// <summary>
    /// 小地图标记组件
    /// 挂载到需要在小地图上显示的游戏对象上
    /// </summary>
    public class MinimapMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [SerializeField] private MinimapMarkerType _markerType = MinimapMarkerType.NPC;
        [SerializeField] private Sprite _customIcon;
        [SerializeField] private Color _iconColor = Color.white;
        [SerializeField, Range(0.5f, 3f)] private float _iconScale = 1f;

        [Header("Visibility")]
        [SerializeField] private bool _alwaysVisible = true;
        [SerializeField, Range(1f, 200f)] private float _visibleRange = 50f;

        [Header("Rotation")]
        [SerializeField] private bool _rotateWithObject = false;

        [Header("Label")]
        [SerializeField] private string _label;
        [SerializeField] private bool _showLabel = false;

        #region Properties

        public MinimapMarkerType MarkerType
        {
            get => _markerType;
            set => _markerType = value;
        }

        public Sprite CustomIcon
        {
            get => _customIcon;
            set => _customIcon = value;
        }

        public Color IconColor
        {
            get => _iconColor;
            set => _iconColor = value;
        }

        public float IconScale
        {
            get => _iconScale;
            set => _iconScale = Mathf.Clamp(value, 0.5f, 3f);
        }

        public bool AlwaysVisible
        {
            get => _alwaysVisible;
            set => _alwaysVisible = value;
        }

        public float VisibleRange
        {
            get => _visibleRange;
            set => _visibleRange = Mathf.Max(1f, value);
        }

        public bool RotateWithObject
        {
            get => _rotateWithObject;
            set => _rotateWithObject = value;
        }

        public string Label
        {
            get => _label;
            set => _label = value;
        }

        public bool ShowLabel
        {
            get => _showLabel;
            set => _showLabel = value;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            MinimapMarkerManager.Register(this);
        }

        private void OnDisable()
        {
            MinimapMarkerManager.Unregister(this);
        }

        #endregion

        #region Public API

        /// <summary>检查是否在指定位置的可见范围内</summary>
        public bool IsVisibleFrom(Vector3 viewerPosition)
        {
            if (_alwaysVisible) return true;

            float distance = Vector3.Distance(transform.position, viewerPosition);
            return distance <= _visibleRange;
        }

        /// <summary>获取当前旋转角度 (用于小地图显示)</summary>
        public float GetRotation()
        {
            return _rotateWithObject ? transform.eulerAngles.y : 0f;
        }

        /// <summary>设置标记类型和图标</summary>
        public void Setup(MinimapMarkerType type, Sprite icon = null, Color? color = null)
        {
            _markerType = type;
            if (icon != null) _customIcon = icon;
            if (color.HasValue) _iconColor = color.Value;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_alwaysVisible)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _visibleRange);
            }
        }
#endif

        #endregion
    }
}
