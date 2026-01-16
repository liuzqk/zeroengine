using System;
using UnityEngine;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 延时步骤 (v1.14.0+)
    /// 等待指定时间后继续
    /// </summary>
    [Serializable]
    public class DelayStep : TutorialStep
    {
        [Header("Delay Settings")]
        [Tooltip("延迟时间 (秒)")]
        public float Duration = 1f;

        [Tooltip("显示进度条")]
        public bool ShowProgress = false;

        [Tooltip("提示文本")]
        public string PromptText;

        public override string StepType => "Delay";

        public override void OnEnter(TutorialContext ctx)
        {
            if (!string.IsNullOrEmpty(PromptText))
            {
                TutorialUIManager.Instance?.ShowPrompt(PromptText, null);
            }

            if (ShowProgress)
            {
                TutorialUIManager.Instance?.ShowProgress(0, 1);
            }
        }

        public override void OnUpdate(TutorialContext ctx)
        {
            if (ShowProgress)
            {
                float progress = Mathf.Clamp01(ctx.StepElapsedTime / Duration);
                TutorialUIManager.Instance?.UpdateProgress(progress);
            }
        }

        public override void OnExit(TutorialContext ctx)
        {
            TutorialUIManager.Instance?.HidePrompt();
            TutorialUIManager.Instance?.HideProgress();
        }

        public override bool IsCompleted(TutorialContext ctx)
        {
            return ctx.StepElapsedTime >= Duration;
        }

        public override string GetDisplayText()
        {
            return !string.IsNullOrEmpty(PromptText)
                ? PromptText
                : $"Wait {Duration}s";
        }

        public override bool Validate(out string error)
        {
            if (Duration <= 0)
            {
                error = "Duration must be positive";
                return false;
            }
            error = null;
            return true;
        }
    }

    /// <summary>
    /// 回调步骤 (v1.14.0+)
    /// 触发自定义回调
    /// </summary>
    [Serializable]
    public class CallbackStep : TutorialStep
    {
        [Header("Callback Settings")]
        [Tooltip("回调标识符")]
        public string CallbackId;

        [Tooltip("等待回调完成")]
        public bool WaitForComplete = false;

        [Tooltip("回调参数")]
        public string[] Parameters;

        // 运行时状态
        [NonSerialized]
        private bool _callbackCompleted;

        // 静态回调注册
        private static event Action<string, string[], Action> OnCallback;

        public override string StepType => "Callback";

        public override void OnEnter(TutorialContext ctx)
        {
            _callbackCompleted = false;

            // 触发回调
            if (!string.IsNullOrEmpty(CallbackId))
            {
                OnCallback?.Invoke(CallbackId, Parameters, () => _callbackCompleted = true);
            }

            if (!WaitForComplete)
            {
                _callbackCompleted = true;
            }
        }

        public override void OnUpdate(TutorialContext ctx)
        {
            // 等待回调完成
        }

        public override void OnExit(TutorialContext ctx)
        {
            // 清理
        }

        public override bool IsCompleted(TutorialContext ctx)
        {
            return _callbackCompleted;
        }

        public override string GetDisplayText()
        {
            return $"Callback: {CallbackId}";
        }

        public override bool Validate(out string error)
        {
            if (string.IsNullOrEmpty(CallbackId))
            {
                error = "CallbackId is required";
                return false;
            }
            error = null;
            return true;
        }

        #region Static API

        /// <summary>
        /// 注册回调处理器
        /// </summary>
        public static void RegisterHandler(Action<string, string[], Action> handler)
        {
            OnCallback += handler;
        }

        /// <summary>
        /// 注销回调处理器
        /// </summary>
        public static void UnregisterHandler(Action<string, string[], Action> handler)
        {
            OnCallback -= handler;
        }

        #endregion
    }
}
