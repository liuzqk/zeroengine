using UnityEngine;
using UnityEngine.UI;

namespace ZeroEngine.UI
{
    /// <summary>
    /// UI工具类
    /// </summary>
    public static class UIUtility
    {
        #region Static Cache - 静态缓存 (性能优化)

        // 复用 RaycastResult 列表，避免每次 IsPointerOverUI 调用时分配
        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
            SharedRaycastResults = new(16);

        // 复用 PointerEventData，避免每次 IsPointerOverUI 调用时分配 (~72 bytes)
        private static UnityEngine.EventSystems.PointerEventData _cachedPointerEventData;

        // 记录 _cachedPointerEventData 所属的 EventSystem
        private static UnityEngine.EventSystems.EventSystem _cachedEventSystem;

        #endregion

        #region CanvasGroup

        /// <summary>
        /// 显示CanvasGroup
        /// </summary>
        public static void ShowCanvasGroup(CanvasGroup cg, bool interactable = true)
        {
            if (cg == null) return;
            cg.alpha = 1f;
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;
        }

        /// <summary>
        /// 隐藏CanvasGroup
        /// </summary>
        public static void HideCanvasGroup(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        /// <summary>
        /// 设置CanvasGroup可见性
        /// </summary>
        public static void SetCanvasGroupVisible(CanvasGroup cg, bool visible, bool interactable = true)
        {
            if (visible)
                ShowCanvasGroup(cg, interactable);
            else
                HideCanvasGroup(cg);
        }

        #endregion

        #region RectTransform

        /// <summary>
        /// 设置锚点为全屏拉伸
        /// </summary>
        public static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 设置锚点为居中
        /// </summary>
        public static void SetCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 获取世界坐标下的Canvas局部位置
        /// </summary>
        public static Vector2 WorldToCanvasPosition(Canvas canvas, Camera camera, Vector3 worldPosition)
        {
            var screenPoint = camera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPoint,
                canvas.worldCamera,
                out var localPoint);
            return localPoint;
        }

        /// <summary>
        /// 屏幕坐标转Canvas局部坐标
        /// </summary>
        public static Vector2 ScreenToCanvasPosition(Canvas canvas, Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.worldCamera,
                out var localPoint);
            return localPoint;
        }

        #endregion

        #region Layout

        /// <summary>
        /// 强制刷新布局
        /// </summary>
        public static void ForceRebuildLayout(RectTransform rect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        /// <summary>
        /// 延迟刷新布局
        /// </summary>
        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        #endregion

        #region ScrollRect

        /// <summary>
        /// 滚动到顶部
        /// </summary>
        public static void ScrollToTop(ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        public static void ScrollToBottom(ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }

        /// <summary>
        /// 滚动到左侧
        /// </summary>
        public static void ScrollToLeft(ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = new Vector2(0, scrollRect.normalizedPosition.y);
        }

        /// <summary>
        /// 滚动到右侧
        /// </summary>
        public static void ScrollToRight(ScrollRect scrollRect)
        {
            scrollRect.normalizedPosition = new Vector2(1, scrollRect.normalizedPosition.y);
        }

        /// <summary>
        /// 滚动到指定子元素
        /// </summary>
        public static void ScrollToChild(ScrollRect scrollRect, RectTransform child)
        {
            Canvas.ForceUpdateCanvases();

            var contentPos = scrollRect.content.anchoredPosition;
            var childPos = (Vector2)scrollRect.transform.InverseTransformPoint(child.position);
            var targetPos = contentPos - childPos;

            scrollRect.content.anchoredPosition = new Vector2(
                scrollRect.horizontal ? targetPos.x : contentPos.x,
                scrollRect.vertical ? targetPos.y : contentPos.y);
        }

        #endregion

        #region Color

        /// <summary>
        /// 设置Graphic的Alpha
        /// </summary>
        public static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        /// <summary>
        /// 设置CanvasGroup的Alpha
        /// </summary>
        public static void SetAlpha(CanvasGroup cg, float alpha)
        {
            if (cg == null) return;
            cg.alpha = alpha;
        }

        /// <summary>
        /// 从十六进制解析颜色
        /// </summary>
        public static Color ParseHexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return Color.white;
        }

        /// <summary>
        /// 颜色转十六进制字符串
        /// </summary>
        public static string ColorToHex(Color color, bool includeAlpha = true)
        {
            return includeAlpha
                ? ColorUtility.ToHtmlStringRGBA(color)
                : ColorUtility.ToHtmlStringRGB(color);
        }

        #endregion

        #region Safe Area

        /// <summary>
        /// 适配安全区域（刘海屏等）
        /// </summary>
        public static void ApplySafeArea(RectTransform rect)
        {
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }

        /// <summary>
        /// 获取安全区域的归一化锚点
        /// </summary>
        public static (Vector2 min, Vector2 max) GetSafeAreaAnchors()
        {
            var safeArea = Screen.safeArea;
            var anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
            var anchorMax = new Vector2((safeArea.x + safeArea.width) / Screen.width,
                                        (safeArea.y + safeArea.height) / Screen.height);
            return (anchorMin, anchorMax);
        }

        #endregion

        #region Raycast

        /// <summary>
        /// 检查点击是否在UI上
        /// </summary>
        public static bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// 检查指定位置是否在UI上
        /// 注意：此方法使用静态复用对象，非线程安全（Unity UI 通常在主线程操作）
        /// </summary>
        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

            // 惰性初始化并复用 PointerEventData（处理 EventSystem 切换的情况）
            if (_cachedPointerEventData == null || _cachedEventSystem != eventSystem)
            {
                _cachedPointerEventData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
                _cachedEventSystem = eventSystem;
            }
            _cachedPointerEventData.position = screenPosition;

            // 使用静态复用列表，避免每次调用分配新 List
            SharedRaycastResults.Clear();
            eventSystem.RaycastAll(_cachedPointerEventData, SharedRaycastResults);

            return SharedRaycastResults.Count > 0;
        }

        #endregion
    }

    /// <summary>
    /// UI扩展方法
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this Component comp) where T : Component
        {
            return comp.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// 设置活动状态（带null检查，避免不必要的SetActive调用）
        /// </summary>
        public static void SetActiveSafe(this GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        /// <summary>
        /// 销毁所有子对象
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 立即销毁所有子对象
        /// </summary>
        public static void DestroyAllChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 设置RectTransform为全屏拉伸
        /// </summary>
        public static void SetStretch(this RectTransform rect)
        {
            UIUtility.SetStretch(rect);
        }

        /// <summary>
        /// 设置RectTransform为居中
        /// </summary>
        public static void SetCenter(this RectTransform rect, Vector2 size)
        {
            UIUtility.SetCenter(rect, size);
        }

        /// <summary>
        /// 强制刷新布局
        /// </summary>
        public static void ForceRebuildLayout(this RectTransform rect)
        {
            UIUtility.ForceRebuildLayout(rect);
        }
    }
}
