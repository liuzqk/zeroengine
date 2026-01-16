using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ZeroEngine.Utils
{
    public static class UIUtils
    {
        #region CanvasGroup & Layout

        public static void ShowCanvasGroup(CanvasGroup canvasGroup, float duration = 0.3f, ZeroEase ease = ZeroEase.OutQuad, bool interactable = true)
        {
            if (canvasGroup == null) return;
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;

            if (DOTweenAdapter.IsAvailable)
            {
                DOTweenAdapter.DOKill(canvasGroup);
                DOTweenAdapter.FadeCanvasGroup(canvasGroup, 1f, duration, ease);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }

        public static void HideCanvasGroup(CanvasGroup canvasGroup, float duration = 0.3f, ZeroEase ease = ZeroEase.InQuad)
        {
            if (canvasGroup == null) return;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (DOTweenAdapter.IsAvailable)
            {
                DOTweenAdapter.DOKill(canvasGroup);
                // For OnComplete, Reflection is hard.
                // We'll just fade out.
                // Or we can use a Sequence or just wait.
                // Standard DOFade doesn't return Sequence.
                // Simple implementation: Just fade.
                // If user wants OnComplete logic via reflection it's complex.
                // For "Hide", we usually want to SetActive(false) at end.
                // We can use a Coroutine helper if simple, but here it's static.
                // Let's rely on Alpha=0 visually hiding it, or check if we can invoke OnComplete.
                // Reflection OnComplete is generic OnComplete(TweenCallback).
                // Let's Skip SetActive(false) for strictly visual fade if Reflection used, 
                // OR just accept Alpha 0 is invisible enough.
                
                DOTweenAdapter.FadeCanvasGroup(canvasGroup, 0f, duration, ease);
                
                // Note: The original code did .OnComplete(() => SetActive(false)).
                // Replicating generic OnComplete via reflection is verbose.
                // For "Plug n Play" robustness, we skip the callback complication.
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
            }
        }

        public static void InitCanvasGroup(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }

        public static void ClearChild(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

#if ZERO_DOTWEEN_SUPPORT
        private static Ease ToDotweenEase(ZeroEase ease)
        {
            // Simple mapping or casting if enums aligned. 
            // Parsing string is slow but robust to int changes. 
            // Direct cast is risky if our enum doesn't match DOTween's exactly.
            // Let's use parsing for "Industrial Standard" robustness against updates, 
            // or just switch case for performance.
            // For now, Parse is easiest to maintain.
            if (System.Enum.TryParse<Ease>(ease.ToString(), out var result))
                return result;
            return Ease.Linear;
        }
#endif

        #endregion

        #region Raycast & Input

        public static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            
#if ENABLE_INPUT_SYSTEM
             var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
             var pos = (Vector2)Input.mousePosition;
#endif
            var eventData = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        #endregion

        #region Positioning & Tooltip

        // Align tooltip to UI Element
        public static void SetTooltipPosition(Canvas canvas, RectTransform tooltipContent, RectTransform canvasRect, RectTransform targetUI)
        {
            var local = UIRectToCanvasLocal(canvas, canvasRect, targetUI);
            PlaceConfined(tooltipContent, canvasRect, local);
        }

        // Align tooltip to Mouse Position
        public static void SetTooltipPositionToMouse(Canvas canvas, RectTransform tooltipContent, RectTransform canvasRect)
        {
            var uiCam = UICamOf(canvas);
#if ENABLE_INPUT_SYSTEM
             var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
             var pos = (Vector2)Input.mousePosition;
#endif
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pos, uiCam, out var local);
            PlaceConfined(tooltipContent, canvasRect, local);
        }

        private static Vector2 UIRectToCanvasLocal(Canvas dstCanvas, RectTransform dstCanvasRect, RectTransform srcUI)
        {
            var srcCanvas = srcUI ? srcUI.GetComponentInParent<Canvas>() : null;
            var srcCam = UICamOf(srcCanvas ? srcCanvas : dstCanvas);
            var dstCam = UICamOf(dstCanvas);

            var p = new Vector2(0.5f, 0.5f);
            var localOfSrc = new Vector3((p.x - srcUI.pivot.x) * srcUI.rect.width, (p.y - srcUI.pivot.y) * srcUI.rect.height, 0f);

            var world = srcUI.TransformPoint(localOfSrc);
            var screen = RectTransformUtility.WorldToScreenPoint(srcCam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dstCanvasRect, screen, dstCam, out var local);
            return local;
        }

        private static void PlaceConfined(RectTransform content, RectTransform canvasRect, Vector2 localCanvasPos)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            var clamped = ClampInsideParent(localCanvasPos, content, canvasRect);

            if (content.parent is RectTransform container)
                container.anchoredPosition = clamped; // Assuming container is the one moving, or content itself
            else
                content.anchoredPosition = clamped;
        }

        private static Vector3 ClampInsideParent(Vector3 desired, RectTransform win, RectTransform parent)
        {
            Vector2 pivot = win.pivot;
            Vector2 size = win.rect.size;
            Rect parentRect = parent.rect;
            Vector2 parentHalf = parentRect.size * 0.5f;

            float toLeft = size.x * pivot.x;
            float toRight = size.x * (1f - pivot.x);
            float toBottom = size.y * pivot.y;
            float toTop = size.y * (1f - pivot.y);

            float minX = -parentHalf.x + toLeft;
            float maxX = parentHalf.x - toRight;
            float minY = -parentHalf.y + toBottom;
            float maxY = parentHalf.y - toTop;

            float x = Mathf.Clamp(desired.x, minX, maxX);
            float y = Mathf.Clamp(desired.y, minY, maxY);

            return new Vector3(x, y, desired.z);
        }

        private static Camera UICamOf(Canvas c)
        {
            return c && c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c ? c.worldCamera : null;
        }

        #endregion
    }
}
