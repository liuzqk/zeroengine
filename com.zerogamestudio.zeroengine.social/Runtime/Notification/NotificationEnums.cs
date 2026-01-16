using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Notification
{
    /// <summary>通知类型</summary>
    public enum NotificationType
    {
        Info,           // 信息
        Success,        // 成功
        Warning,        // 警告
        Error,          // 错误
        Achievement,    // 成就
        Reward,         // 奖励
        Quest,          // 任务
        System,         // 系统
        Social,         // 社交
        Custom          // 自定义
    }

    /// <summary>通知优先级</summary>
    public enum NotificationPriority
    {
        Low = 0,
        Normal = 50,
        High = 100,
        Critical = 200
    }

    /// <summary>通知位置</summary>
    public enum NotificationPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center
    }

    /// <summary>通知动画</summary>
    public enum NotificationAnimation
    {
        None,
        FadeIn,
        SlideIn,
        PopIn,
        Bounce
    }

    /// <summary>通知事件类型</summary>
    public enum NotificationEventType
    {
        Shown,
        Hidden,
        Clicked,
        Expired,
        Queued
    }

    /// <summary>通知数据</summary>
    [Serializable]
    public class NotificationData
    {
        // 唯一ID
        [NonSerialized] public string Id;

        // 内容
        public string Title;
        public string Message;
        public Sprite Icon;

        // 类型和优先级
        public NotificationType Type = NotificationType.Info;
        public NotificationPriority Priority = NotificationPriority.Normal;

        // 显示设置
        public NotificationPosition Position = NotificationPosition.TopRight;
        public NotificationAnimation Animation = NotificationAnimation.SlideIn;
        public float Duration = 3f;  // 0 = 手动关闭
        public bool Closable = true;
        public bool Clickable = false;

        // 回调
        [NonSerialized] public Action OnClick;
        [NonSerialized] public Action OnClose;

        // 自定义数据
        public string CustomData;
        public GameObject CustomPrefab;

        // 时间戳
        [NonSerialized] public float ShowTime;
        [NonSerialized] public float ExpireTime;

        public NotificationData() { }

        public NotificationData(string title, string message, NotificationType type = NotificationType.Info)
        {
            Title = title;
            Message = message;
            Type = type;
        }

        public bool IsExpired => Duration > 0 && Time.unscaledTime >= ExpireTime;
    }

    /// <summary>通知配置</summary>
    [Serializable]
    public class NotificationConfig
    {
        [Header("显示设置")]
        public int MaxVisibleCount = 5;
        public float DefaultDuration = 3f;
        public float StackOffset = 10f;

        [Header("动画设置")]
        public float AnimationDuration = 0.3f;
        public AnimationCurve AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("音效")]
        public bool PlaySound = true;
        public string DefaultSoundId = "notification";

        [Header("过滤")]
        public NotificationPriority MinPriority = NotificationPriority.Low;
        public List<NotificationType> DisabledTypes = new List<NotificationType>();
    }

    /// <summary>通知事件参数</summary>
    public class NotificationEventArgs
    {
        public NotificationEventType Type { get; private set; }
        public NotificationData Notification { get; private set; }

        public static NotificationEventArgs Shown(NotificationData notification)
            => new() { Type = NotificationEventType.Shown, Notification = notification };

        public static NotificationEventArgs Hidden(NotificationData notification)
            => new() { Type = NotificationEventType.Hidden, Notification = notification };

        public static NotificationEventArgs Clicked(NotificationData notification)
            => new() { Type = NotificationEventType.Clicked, Notification = notification };

        public static NotificationEventArgs Expired(NotificationData notification)
            => new() { Type = NotificationEventType.Expired, Notification = notification };

        public static NotificationEventArgs Queued(NotificationData notification)
            => new() { Type = NotificationEventType.Queued, Notification = notification };
    }

    /// <summary>通知构建器 (流式API)</summary>
    public class NotificationBuilder
    {
        private readonly NotificationData _data = new NotificationData();

        public NotificationBuilder(string title, string message)
        {
            _data.Title = title;
            _data.Message = message;
        }

        public NotificationBuilder SetType(NotificationType type) { _data.Type = type; return this; }
        public NotificationBuilder SetPriority(NotificationPriority priority) { _data.Priority = priority; return this; }
        public NotificationBuilder SetPosition(NotificationPosition position) { _data.Position = position; return this; }
        public NotificationBuilder SetDuration(float duration) { _data.Duration = duration; return this; }
        public NotificationBuilder SetIcon(Sprite icon) { _data.Icon = icon; return this; }
        public NotificationBuilder SetClosable(bool closable) { _data.Closable = closable; return this; }
        public NotificationBuilder SetClickable(bool clickable) { _data.Clickable = clickable; return this; }
        public NotificationBuilder OnClick(Action callback) { _data.OnClick = callback; _data.Clickable = true; return this; }
        public NotificationBuilder OnClose(Action callback) { _data.OnClose = callback; return this; }
        public NotificationBuilder SetCustomData(string data) { _data.CustomData = data; return this; }
        public NotificationBuilder SetAnimation(NotificationAnimation anim) { _data.Animation = anim; return this; }

        public NotificationData Build() => _data;

        public void Show()
        {
            NotificationManager.Instance?.Show(_data);
        }
    }
}