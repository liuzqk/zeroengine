using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Notification
{
    /// <summary>
    /// 通知系统管理器
    /// </summary>
    public class NotificationManager : MonoSingleton<NotificationManager>
    {
        [Header("配置")]
        [SerializeField] private NotificationConfig _config = new NotificationConfig();

        [Header("UI")]
        [SerializeField] private Transform _notificationContainer;
        [SerializeField] private GameObject _defaultPrefab;

        [Header("调试")]
        [SerializeField] private bool _debugMode;

        // 通知队列
        private readonly Queue<NotificationData> _pendingQueue = new Queue<NotificationData>();
        private readonly List<NotificationData> _activeNotifications = new List<NotificationData>();
        private readonly List<NotificationData> _toRemove = new List<NotificationData>();

        // ID生成
        private int _nextId;

        #region Events

        public event Action<NotificationEventArgs> OnNotificationEvent;

        #endregion

        #region Properties

        public NotificationConfig Config => _config;
        public int ActiveCount => _activeNotifications.Count;
        public int PendingCount => _pendingQueue.Count;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // 处理过期通知
            ProcessExpiredNotifications();

            // 处理待显示队列
            ProcessPendingQueue();
        }

        #endregion

        #region Public API - Show

        /// <summary>显示通知</summary>
        public string Show(NotificationData notification)
        {
            if (notification == null) return null;

            // 检查过滤
            if (notification.Priority < _config.MinPriority)
            {
                Log($"通知被过滤 (优先级太低): {notification.Title}");
                return null;
            }

            if (_config.DisabledTypes.Contains(notification.Type))
            {
                Log($"通知被过滤 (类型禁用): {notification.Title}");
                return null;
            }

            // 生成ID
            notification.Id = $"notification_{_nextId++}";

            // 检查是否可以直接显示
            if (_activeNotifications.Count < _config.MaxVisibleCount)
            {
                ShowNotificationInternal(notification);
            }
            else
            {
                // 加入队列
                _pendingQueue.Enqueue(notification);
                OnNotificationEvent?.Invoke(NotificationEventArgs.Queued(notification));
                Log($"通知加入队列: {notification.Title}");
            }

            return notification.Id;
        }

        /// <summary>显示简单通知</summary>
        public string Show(string title, string message, NotificationType type = NotificationType.Info)
        {
            return Show(new NotificationData(title, message, type));
        }

        /// <summary>显示成功通知</summary>
        public string ShowSuccess(string title, string message)
            => Show(title, message, NotificationType.Success);

        /// <summary>显示警告通知</summary>
        public string ShowWarning(string title, string message)
            => Show(title, message, NotificationType.Warning);

        /// <summary>显示错误通知</summary>
        public string ShowError(string title, string message)
            => Show(title, message, NotificationType.Error);

        /// <summary>显示成就通知</summary>
        public string ShowAchievement(string title, string message, Sprite icon = null)
        {
            var notification = new NotificationData(title, message, NotificationType.Achievement)
            {
                Icon = icon,
                Duration = 5f,
                Priority = NotificationPriority.High
            };
            return Show(notification);
        }

        /// <summary>显示奖励通知</summary>
        public string ShowReward(string title, string message, Sprite icon = null)
        {
            var notification = new NotificationData(title, message, NotificationType.Reward)
            {
                Icon = icon,
                Duration = 4f
            };
            return Show(notification);
        }

        /// <summary>创建通知构建器</summary>
        public static NotificationBuilder Create(string title, string message)
        {
            return new NotificationBuilder(title, message);
        }

        #endregion

        #region Public API - Control

        /// <summary>隐藏指定通知</summary>
        public void Hide(string notificationId)
        {
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                if (_activeNotifications[i].Id == notificationId)
                {
                    HideNotificationInternal(_activeNotifications[i]);
                    _activeNotifications.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>隐藏所有通知</summary>
        public void HideAll()
        {
            foreach (var notification in _activeNotifications)
            {
                HideNotificationInternal(notification);
            }
            _activeNotifications.Clear();
            _pendingQueue.Clear();
        }

        /// <summary>隐藏指定类型的所有通知</summary>
        public void HideAllOfType(NotificationType type)
        {
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                if (_activeNotifications[i].Type == type)
                {
                    HideNotificationInternal(_activeNotifications[i]);
                    _activeNotifications.RemoveAt(i);
                }
            }
        }

        /// <summary>点击通知</summary>
        public void ClickNotification(string notificationId)
        {
            foreach (var notification in _activeNotifications)
            {
                if (notification.Id == notificationId)
                {
                    notification.OnClick?.Invoke();
                    OnNotificationEvent?.Invoke(NotificationEventArgs.Clicked(notification));
                    Log($"通知被点击: {notification.Title}");

                    // 点击后隐藏
                    Hide(notificationId);
                    return;
                }
            }
        }

        #endregion

        #region Public API - Config

        /// <summary>设置最大显示数量</summary>
        public void SetMaxVisibleCount(int count)
        {
            _config.MaxVisibleCount = Mathf.Max(1, count);
        }

        /// <summary>设置默认持续时间</summary>
        public void SetDefaultDuration(float duration)
        {
            _config.DefaultDuration = Mathf.Max(0, duration);
        }

        /// <summary>禁用通知类型</summary>
        public void DisableType(NotificationType type)
        {
            if (!_config.DisabledTypes.Contains(type))
                _config.DisabledTypes.Add(type);
        }

        /// <summary>启用通知类型</summary>
        public void EnableType(NotificationType type)
        {
            _config.DisabledTypes.Remove(type);
        }

        /// <summary>设置最低优先级</summary>
        public void SetMinPriority(NotificationPriority priority)
        {
            _config.MinPriority = priority;
        }

        #endregion

        #region Internal

        private void ShowNotificationInternal(NotificationData notification)
        {
            notification.ShowTime = Time.unscaledTime;
            notification.ExpireTime = notification.Duration > 0
                ? Time.unscaledTime + notification.Duration
                : float.MaxValue;

            _activeNotifications.Add(notification);

            // TODO: 创建UI实例
            CreateNotificationUI(notification);

            // 播放音效
            if (_config.PlaySound)
            {
                // TODO: 播放音效
            }

            OnNotificationEvent?.Invoke(NotificationEventArgs.Shown(notification));
            Log($"显示通知: {notification.Title}");
        }

        private void HideNotificationInternal(NotificationData notification)
        {
            notification.OnClose?.Invoke();

            // TODO: 销毁UI实例
            DestroyNotificationUI(notification);

            OnNotificationEvent?.Invoke(NotificationEventArgs.Hidden(notification));
            Log($"隐藏通知: {notification.Title}");
        }

        private void ProcessExpiredNotifications()
        {
            _toRemove.Clear();

            foreach (var notification in _activeNotifications)
            {
                if (notification.IsExpired)
                {
                    _toRemove.Add(notification);
                }
            }

            foreach (var notification in _toRemove)
            {
                OnNotificationEvent?.Invoke(NotificationEventArgs.Expired(notification));
                HideNotificationInternal(notification);
                _activeNotifications.Remove(notification);
            }
        }

        private void ProcessPendingQueue()
        {
            while (_pendingQueue.Count > 0 && _activeNotifications.Count < _config.MaxVisibleCount)
            {
                var notification = _pendingQueue.Dequeue();
                ShowNotificationInternal(notification);
            }
        }

        private void CreateNotificationUI(NotificationData notification)
        {
            // TODO: 实例化通知预制体
        }

        private void DestroyNotificationUI(NotificationData notification)
        {
            // TODO: 销毁通知UI
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode) Debug.Log($"[Notification] {message}");
        }

        #endregion
    }
}