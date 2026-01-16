using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

#if DOTWEEN
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
#endif

namespace ZeroEngine.UI
{
    /// <summary>
    /// UI视图基类 - 工业级通用面板基类
    ///
    /// 生命周期:
    /// OnCreate -> OnOpen -> OnResume -> OnPause -> OnClose -> OnDestroy
    ///
    /// OnCreate: 首次创建时调用（只调用一次）
    /// OnOpen: 每次打开时调用
    /// OnResume: 从暂停恢复时调用（被其他面板覆盖后恢复）
    /// OnPause: 被其他面板覆盖时调用
    /// OnClose: 关闭时调用
    /// OnDestroy: 销毁时调用（只调用一次）
    /// </summary>
    public abstract class UIViewBase : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// 缓存的视图名称（避免每次 GetType().Name 分配）
        /// </summary>
        private string _cachedViewName;

        /// <summary>
        /// 视图名称（唯一标识）
        /// </summary>
        public virtual string ViewName => _cachedViewName ??= GetType().Name;

        /// <summary>
        /// 视图配置
        /// </summary>
        public UIViewConfig Config { get; private set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public UIViewState State { get; private set; } = UIViewState.None;

        /// <summary>
        /// 打开参数
        /// </summary>
        protected UIOpenArgs OpenArgs { get; private set; }

        /// <summary>
        /// CanvasGroup组件
        /// </summary>
        protected CanvasGroup CanvasGroup { get; private set; }

        /// <summary>
        /// RectTransform
        /// </summary>
        protected RectTransform RectTransform { get; private set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible => State == UIViewState.Opened || State == UIViewState.Opening;

        /// <summary>
        /// 最后选中的对象（用于焦点恢复）
        /// </summary>
        private GameObject _lastSelected;

        /// <summary>
        /// 默认选中对象
        /// </summary>
        [SerializeField] protected GameObject defaultSelected;

        #endregion

        #region Initialization

        /// <summary>
        /// 内部初始化（UIManager调用）
        /// </summary>
        internal void InternalInit(UIViewConfig config)
        {
            Config = config;
            CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            RectTransform = GetComponent<RectTransform>();

            // 初始状态隐藏
            SetVisible(false, true);
            State = UIViewState.Created;
        }

        /// <summary>
        /// 异步初始化（子类重写）
        /// </summary>
        public virtual Task OnCreateAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Lifecycle - 生命周期

        /// <summary>
        /// 首次创建
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// 打开（每次）
        /// </summary>
        protected virtual void OnOpen() { }

        /// <summary>
        /// 从暂停恢复
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 暂停（被覆盖）
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 关闭
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// 销毁
        /// </summary>
        protected virtual void OnViewDestroy() { }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public virtual void Refresh() { }

        /// <summary>
        /// 本地化刷新
        /// </summary>
        protected virtual void OnLocalizationChanged() { }

        #endregion

        #region Internal Lifecycle - 内部生命周期（UIManager调用）

        internal async Task InternalOpenAsync(UIOpenArgs args)
        {
            OpenArgs = args;

            if (State == UIViewState.Created)
            {
                await OnCreateAsync();
                OnCreate();
            }

            State = UIViewState.Opening;
            gameObject.SetActive(true);
            KillActiveAnimations();

            // 播放打开动画
            if (!args.Immediate && Config.openAnimation != UIAnimationType.None)
            {
                await PlayOpenAnimation();
            }
            else
            {
                SetVisible(true, true);
            }

            State = UIViewState.Opened;
            OnOpen();

            // 设置焦点
            RestoreFocus();

            args.OnOpened?.Invoke();
        }

        internal async Task InternalCloseAsync(UICloseArgs args)
        {
            if (State == UIViewState.Closed || State == UIViewState.Closing)
                return;

            State = UIViewState.Closing;
            SaveLastSelected();
            KillActiveAnimations();

            OnClose();

            // 播放关闭动画
            if (!args.Immediate && Config.closeAnimation != UIAnimationType.None)
            {
                await PlayCloseAnimation();
            }
            else
            {
                SetVisible(false, true);
            }

            State = UIViewState.Closed;
            gameObject.SetActive(false);

            OpenArgs?.OnClosed?.Invoke();
        }

        internal void InternalPause()
        {
            if (State != UIViewState.Opened) return;

            SaveLastSelected();
            State = UIViewState.Paused;
            OnPause();

            // 禁用交互
            if (CanvasGroup != null)
            {
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }
        }

        internal void InternalResume()
        {
            if (State != UIViewState.Paused) return;

            State = UIViewState.Opened;
            OnResume();

            // 启用交互
            if (CanvasGroup != null)
            {
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = Config.blockInput;
            }

            RestoreFocus();
        }

        internal void InternalDestroy()
        {
            OnViewDestroy();
            State = UIViewState.None;
        }

        #endregion

        #region Animation - 动画

        protected virtual async Task PlayOpenAnimation()
        {
            SetVisible(true, false);

            switch (Config.openAnimation)
            {
                case UIAnimationType.Fade:
                    await AnimateFade(0f, 1f, Config.animationDuration);
                    break;
                case UIAnimationType.Scale:
                    await AnimateScale(Vector3.zero, Vector3.one, Config.animationDuration);
                    break;
                case UIAnimationType.SlideLeft:
                    await AnimateSlide(new Vector2(-Screen.width, 0), Vector2.zero, Config.animationDuration);
                    break;
                case UIAnimationType.SlideRight:
                    await AnimateSlide(new Vector2(Screen.width, 0), Vector2.zero, Config.animationDuration);
                    break;
                case UIAnimationType.SlideTop:
                    await AnimateSlide(new Vector2(0, Screen.height), Vector2.zero, Config.animationDuration);
                    break;
                case UIAnimationType.SlideBottom:
                    await AnimateSlide(new Vector2(0, -Screen.height), Vector2.zero, Config.animationDuration);
                    break;
                case UIAnimationType.Custom:
                    await PlayCustomOpenAnimation();
                    break;
            }

            SetVisible(true, true);
        }

        protected virtual async Task PlayCloseAnimation()
        {
            switch (Config.closeAnimation)
            {
                case UIAnimationType.Fade:
                    await AnimateFade(1f, 0f, Config.animationDuration);
                    break;
                case UIAnimationType.Scale:
                    await AnimateScale(Vector3.one, Vector3.zero, Config.animationDuration);
                    break;
                case UIAnimationType.SlideLeft:
                    await AnimateSlide(Vector2.zero, new Vector2(-Screen.width, 0), Config.animationDuration);
                    break;
                case UIAnimationType.SlideRight:
                    await AnimateSlide(Vector2.zero, new Vector2(Screen.width, 0), Config.animationDuration);
                    break;
                case UIAnimationType.SlideTop:
                    await AnimateSlide(Vector2.zero, new Vector2(0, Screen.height), Config.animationDuration);
                    break;
                case UIAnimationType.SlideBottom:
                    await AnimateSlide(Vector2.zero, new Vector2(0, -Screen.height), Config.animationDuration);
                    break;
                case UIAnimationType.Custom:
                    await PlayCustomCloseAnimation();
                    break;
            }

            SetVisible(false, true);
        }

        /// <summary>
        /// 自定义打开动画（子类重写）
        /// </summary>
        protected virtual Task PlayCustomOpenAnimation() => Task.CompletedTask;

        /// <summary>
        /// 自定义关闭动画（子类重写）
        /// </summary>
        protected virtual Task PlayCustomCloseAnimation() => Task.CompletedTask;

#if DOTWEEN
        // DOTween 实现 - 零 GC 分配
        private Task AnimateFade(float from, float to, float duration)
        {
            if (CanvasGroup == null) return Task.CompletedTask;

            CanvasGroup.alpha = from;
            return DOTween.To(() => CanvasGroup.alpha, x => CanvasGroup.alpha = x, to, duration)
                .SetUpdate(true)
                .SetEase(Ease.Linear)
                .AsTask();
        }

        private Task AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            transform.localScale = from;
            return transform.DOScale(to, duration)
                .SetUpdate(true)
                .SetEase(Ease.OutBack)
                .AsTask();
        }

        private Task AnimateSlide(Vector2 from, Vector2 to, float duration)
        {
            if (RectTransform == null) return Task.CompletedTask;

            RectTransform.anchoredPosition = from;
            return DOTween.To(() => RectTransform.anchoredPosition, x => RectTransform.anchoredPosition = x, to, duration)
                .SetUpdate(true)
                .SetEase(Ease.OutCubic)
                .AsTask();
        }

        /// <summary>
        /// 清理当前动画（防止动画冲突）
        /// </summary>
        protected void KillActiveAnimations()
        {
            DOTween.Kill(transform);
            if (CanvasGroup != null)
                DOTween.Kill(CanvasGroup);
            if (RectTransform != null)
                DOTween.Kill(RectTransform);
        }
#else
        // 原生 async/await 实现 - 无 DOTween 依赖时的后备方案
        private async Task AnimateFade(float from, float to, float duration)
        {
            if (CanvasGroup == null) return;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                CanvasGroup.alpha = Mathf.Lerp(from, to, t);
                await Task.Yield();
            }

            CanvasGroup.alpha = to;
        }

        private async Task AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutBack(t);
                transform.localScale = Vector3.Lerp(from, to, t);
                await Task.Yield();
            }

            transform.localScale = to;
        }

        private async Task AnimateSlide(Vector2 from, Vector2 to, float duration)
        {
            if (RectTransform == null) return;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutCubic(t);
                RectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
                await Task.Yield();
            }

            RectTransform.anchoredPosition = to;
        }

        // 缓动函数（仅非 DOTween 时需要）
        private float EaseOutBack(float t) => 1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
        private float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);

        protected void KillActiveAnimations() { }
#endif

        #endregion

        #region Visibility - 可见性

        protected void SetVisible(bool visible, bool immediate = false)
        {
            if (CanvasGroup == null) return;

            if (immediate)
            {
                CanvasGroup.alpha = visible ? 1f : 0f;
            }

            CanvasGroup.interactable = visible;
            CanvasGroup.blocksRaycasts = visible && Config.blockInput;
        }

        #endregion

        #region Focus - 焦点管理

        /// <summary>
        /// 恢复焦点
        /// </summary>
        public virtual void RestoreFocus()
        {
            if (!gameObject.activeInHierarchy) return;
            if (EventSystem.current == null) return;

            // 优先恢复上次选中
            if (_lastSelected != null && _lastSelected.activeInHierarchy)
            {
                SetSelected(_lastSelected);
                return;
            }

            // 其次使用默认选中
            if (defaultSelected != null && defaultSelected.activeInHierarchy)
            {
                SetSelected(defaultSelected);
                return;
            }

            // 最后查找第一个可选中对象
            var selectable = GetComponentInChildren<UnityEngine.UI.Selectable>(true);
            if (selectable != null && selectable.gameObject.activeInHierarchy)
            {
                SetSelected(selectable.gameObject);
            }
        }

        /// <summary>
        /// 保存当前选中
        /// </summary>
        public void SaveLastSelected()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(transform))
            {
                _lastSelected = current;
            }
        }

        /// <summary>
        /// 设置选中对象
        /// </summary>
        protected void SetSelected(GameObject go)
        {
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(go);
            }
        }

        /// <summary>
        /// 清除选中
        /// </summary>
        protected void ClearSelected()
        {
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        #endregion

        #region Public API - 公共接口

        /// <summary>
        /// 关闭当前面板
        /// </summary>
        public void Close()
        {
            UIManager.Instance?.Close(ViewName);
        }

        /// <summary>
        /// 关闭并返回结果
        /// </summary>
        public void CloseWithResult(object result)
        {
            UIManager.Instance?.Close(ViewName, UICloseArgs.Create(result));
        }

        /// <summary>
        /// 获取打开时传入的数据
        /// </summary>
        protected T GetData<T>()
        {
            if (OpenArgs?.Data is T data)
                return data;
            return default;
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void OnDestroy()
        {
            KillActiveAnimations();
            InternalDestroy();
        }

        #endregion
    }
#if DOTWEEN
    /// <summary>
    /// DOTween 桥接扩展
    /// </summary>
    public static class DOTweenExtensions
    {
        public static Task AsTask(this Tween tween)
        {
            if (tween == null) return Task.CompletedTask;
            if (tween.IsComplete()) return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();
            tween.OnComplete(() => tcs.TrySetResult(true));
            tween.OnKill(() => tcs.TrySetResult(false));
            return tcs.Task;
        }
    }
#endif
}
