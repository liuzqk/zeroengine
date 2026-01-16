using System;
using UnityEngine;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 教程步骤基类 (v1.14.0+)
    /// 使用 [SerializeReference] 实现多态序列化
    /// </summary>
    [Serializable]
    public abstract class TutorialStep
    {
        #region Serialized Fields

        [Tooltip("步骤 ID (可选，用于跳转)")]
        public string StepId;

        [Tooltip("步骤描述 (仅编辑器显示)")]
        public string Description;

        [Tooltip("是否可跳过")]
        public bool CanSkip = true;

        [Tooltip("自动完成延迟 (秒，0 表示不自动完成)")]
        public float AutoCompleteDelay = 0f;

        [HideInInspector]
        [Tooltip("编辑器位置 (GraphView 用)")]
        public Vector2 EditorPosition;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 步骤类型名称 (用于编辑器显示)
        /// </summary>
        public abstract string StepType { get; }

        /// <summary>
        /// 步骤进入时调用
        /// </summary>
        public abstract void OnEnter(TutorialContext ctx);

        /// <summary>
        /// 每帧更新时调用
        /// </summary>
        public abstract void OnUpdate(TutorialContext ctx);

        /// <summary>
        /// 步骤退出时调用
        /// </summary>
        public abstract void OnExit(TutorialContext ctx);

        /// <summary>
        /// 检查步骤是否完成
        /// </summary>
        public abstract bool IsCompleted(TutorialContext ctx);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 获取步骤简短描述 (用于 UI 显示)
        /// </summary>
        public virtual string GetDisplayText()
        {
            return Description ?? StepType;
        }

        /// <summary>
        /// 当步骤被跳过时调用
        /// </summary>
        public virtual void OnSkip(TutorialContext ctx)
        {
            OnExit(ctx);
        }

        /// <summary>
        /// 验证步骤配置 (编辑器用)
        /// </summary>
        public virtual bool Validate(out string error)
        {
            error = null;
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 对话步骤 (v1.14.0+)
    /// </summary>
    [Serializable]
    public class DialogueStep : TutorialStep
    {
        [Header("Dialogue")]
        [Tooltip("说话者名称")]
        public string SpeakerName;

        [Tooltip("对话内容")]
        [TextArea(2, 5)]
        public string DialogueText;

        [Tooltip("说话者头像")]
        public Sprite SpeakerIcon;

        [Tooltip("对话位置")]
        public DialoguePosition Position = DialoguePosition.Bottom;

        [Tooltip("打字机效果速度 (字符/秒，0 禁用)")]
        public float TypewriterSpeed = 30f;

        [Tooltip("等待玩家确认")]
        public bool WaitForConfirm = true;

        [Tooltip("确认按键")]
        public KeyCode ConfirmKey = KeyCode.Space;

        // 运行时状态
        [NonSerialized]
        private bool _isConfirmed;
        [NonSerialized]
        private bool _typewriterComplete;

        public override string StepType => "Dialogue";

        public override void OnEnter(TutorialContext ctx)
        {
            _isConfirmed = false;
            _typewriterComplete = TypewriterSpeed <= 0;

            // 通知 UI 显示对话
            TutorialUIManager.Instance?.ShowDialogue(DialogueText, SpeakerName, SpeakerIcon);
        }

        public override void OnUpdate(TutorialContext ctx)
        {
            // 检测确认输入
            if (WaitForConfirm && _typewriterComplete && Input.GetKeyDown(ConfirmKey))
            {
                _isConfirmed = true;
            }
        }

        public override void OnExit(TutorialContext ctx)
        {
            TutorialUIManager.Instance?.HideDialogue();
        }

        public override bool IsCompleted(TutorialContext ctx)
        {
            if (!WaitForConfirm)
            {
                return _typewriterComplete;
            }

            return _isConfirmed;
        }

        private void OnTypewriterComplete()
        {
            _typewriterComplete = true;
        }

        private void OnConfirm()
        {
            _isConfirmed = true;
        }

        public override string GetDisplayText()
        {
            return !string.IsNullOrEmpty(SpeakerName)
                ? $"{SpeakerName}: {DialogueText}"
                : DialogueText;
        }
    }

    /// <summary>
    /// 对话位置
    /// </summary>
    public enum DialoguePosition
    {
        Top,
        Bottom,
        Center,
        Custom
    }

    /// <summary>
    /// 高亮步骤 (v1.14.0+)
    /// </summary>
    [Serializable]
    public class HighlightStep : TutorialStep
    {
        [Header("Highlight")]
        [Tooltip("目标路径 (UI 路径或 GameObject 名称)")]
        public string TargetPath;

        [Tooltip("高亮类型")]
        public HighlightType HighlightType = HighlightType.Circle;

        [Tooltip("提示文本")]
        [TextArea(1, 3)]
        public string HintText;

        [Tooltip("提示文本位置偏移")]
        public Vector2 HintOffset = Vector2.zero;

        [Tooltip("等待点击目标")]
        public bool WaitForClick = true;

        [Tooltip("超时时间 (秒，0 无限等待)")]
        public float Timeout = 0f;

        // 运行时状态
        [NonSerialized]
        private bool _targetClicked;
        [NonSerialized]
        private RectTransform _target;

        public override string StepType => "Highlight";

        public override void OnEnter(TutorialContext ctx)
        {
            _targetClicked = false;

            // 查找目标
            _target = ctx.FindUITarget(TargetPath);

            if (_target != null)
            {
                TutorialUIManager.Instance?.ShowHighlight(_target, HighlightType, HintText);
            }
            else
            {
                Debug.LogWarning($"[ZeroEngine.Tutorial] Highlight target not found: {TargetPath}");
            }
        }

        public override void OnUpdate(TutorialContext ctx)
        {
            // 超时检查
            if (Timeout > 0 && ctx.StepElapsedTime >= Timeout)
            {
                _targetClicked = true;
            }
        }

        public override void OnExit(TutorialContext ctx)
        {
            TutorialUIManager.Instance?.HideHighlight();
        }

        public override bool IsCompleted(TutorialContext ctx)
        {
            if (!WaitForClick)
            {
                return true;
            }

            return _targetClicked;
        }

        private void OnTargetClicked()
        {
            _targetClicked = true;
        }

        public override bool Validate(out string error)
        {
            if (string.IsNullOrEmpty(TargetPath))
            {
                error = "Target path is required";
                return false;
            }
            error = null;
            return true;
        }
    }
}
