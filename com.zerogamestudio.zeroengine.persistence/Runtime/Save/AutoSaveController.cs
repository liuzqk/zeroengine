using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZeroEngine.Save
{
    /// <summary>
    /// 自动存档控制器
    /// 根据配置的触发条件自动执行存档
    /// </summary>
    public class AutoSaveController : MonoBehaviour
    {
        private SaveSlotManager _slotManager;
        private SaveSystemConfig _config;
        private float _lastAutoSaveTime;
        private bool _isEnabled;
        private bool _isPaused;

        /// <summary>
        /// 自动存档触发时的回调
        /// </summary>
        public event Action<AutoSaveTrigger> OnAutoSaveTriggered;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(SaveSlotManager slotManager, SaveSystemConfig config)
        {
            _slotManager = slotManager;
            _config = config;
            _lastAutoSaveTime = Time.realtimeSinceStartup;
            _isEnabled = config.EnableAutoSave;

            if (_isEnabled)
            {
                SubscribeEvents();
            }
        }

        /// <summary>
        /// 启用自动存档
        /// </summary>
        public void Enable()
        {
            if (_isEnabled) return;
            _isEnabled = true;
            _lastAutoSaveTime = Time.realtimeSinceStartup;
            SubscribeEvents();
        }

        /// <summary>
        /// 禁用自动存档
        /// </summary>
        public void Disable()
        {
            if (!_isEnabled) return;
            _isEnabled = false;
            UnsubscribeEvents();
        }

        /// <summary>
        /// 暂停自动存档（加载中、战斗中等）
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 恢复自动存档
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            _lastAutoSaveTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 手动触发自动存档（用于自定义事件）
        /// </summary>
        public void TriggerAutoSave(AutoSaveTrigger trigger = AutoSaveTrigger.ImportantEvent)
        {
            if (!CanAutoSave(trigger)) return;
            ExecuteAutoSave(trigger);
        }

        private void Update()
        {
            if (!_isEnabled || _isPaused) return;

            // 定时自动存档
            if ((_config.AutoSaveTriggers & AutoSaveTrigger.Interval) != 0)
            {
                if (Time.realtimeSinceStartup - _lastAutoSaveTime >= _config.AutoSaveInterval)
                {
                    ExecuteAutoSave(AutoSaveTrigger.Interval);
                }
            }
        }

        private void SubscribeEvents()
        {
            // 场景切换
            if ((_config.AutoSaveTriggers & AutoSaveTrigger.SceneChange) != 0)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            // 应用暂停/退出
            if ((_config.AutoSaveTriggers & AutoSaveTrigger.OnPause) != 0)
            {
                Application.focusChanged += OnApplicationFocusChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.focusChanged -= OnApplicationFocusChanged;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 跳过叠加场景
            if (mode == LoadSceneMode.Additive) return;

            // 延迟一帧执行，确保场景完全加载
            StartCoroutine(DelayedAutoSave(AutoSaveTrigger.SceneChange));
        }

        private System.Collections.IEnumerator DelayedAutoSave(AutoSaveTrigger trigger)
        {
            yield return null; // 等待一帧

            if (CanAutoSave(trigger))
            {
                ExecuteAutoSave(trigger);
            }
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            // 失去焦点时自动存档
            if (!hasFocus && CanAutoSave(AutoSaveTrigger.OnPause))
            {
                ExecuteAutoSave(AutoSaveTrigger.OnPause);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 移动平台暂停时存档
            if (pauseStatus && CanAutoSave(AutoSaveTrigger.OnPause))
            {
                ExecuteAutoSave(AutoSaveTrigger.OnPause);
            }
        }

        private bool CanAutoSave(AutoSaveTrigger trigger)
        {
            if (!_isEnabled) return false;
            if (_isPaused) return false;
            if (_slotManager == null) return false;
            if ((_config.AutoSaveTriggers & trigger) == 0) return false;

            return true;
        }

        private void ExecuteAutoSave(AutoSaveTrigger trigger)
        {
            _lastAutoSaveTime = Time.realtimeSinceStartup;

            Debug.Log($"[AutoSave] Triggered: {trigger}");

            OnAutoSaveTriggered?.Invoke(trigger);

            _slotManager.AutoSave(success =>
            {
                if (success)
                {
                    Debug.Log($"[AutoSave] Completed: {trigger}");
                }
                else
                {
                    Debug.LogWarning($"[AutoSave] Failed: {trigger}");
                }
            });
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #region Quest/Event Integration

        /// <summary>
        /// 任务完成时调用（由 QuestManager 触发）
        /// </summary>
        public void OnQuestCompleted(string questId)
        {
            if (CanAutoSave(AutoSaveTrigger.QuestComplete))
            {
                Debug.Log($"[AutoSave] Quest completed: {questId}");
                ExecuteAutoSave(AutoSaveTrigger.QuestComplete);
            }
        }

        /// <summary>
        /// 重要事件发生时调用（游戏逻辑触发）
        /// </summary>
        public void OnImportantEvent(string eventName)
        {
            if (CanAutoSave(AutoSaveTrigger.ImportantEvent))
            {
                Debug.Log($"[AutoSave] Important event: {eventName}");
                ExecuteAutoSave(AutoSaveTrigger.ImportantEvent);
            }
        }

        #endregion
    }
}
