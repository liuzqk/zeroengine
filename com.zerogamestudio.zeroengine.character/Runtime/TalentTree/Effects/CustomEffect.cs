using System;
using UnityEngine;
using UnityEngine.Events;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 自定义效果
    /// 通过回调 ID 触发外部系统
    /// </summary>
    [Serializable]
    public class CustomEffect : TalentEffect
    {
        [Header("回调配置")]
        [Tooltip("回调 ID（用于外部系统识别）")]
        public string CallbackId;

        [Tooltip("回调参数")]
        public string Parameter;

        /// <summary>
        /// 全局回调事件（项目层订阅）
        /// </summary>
        public static event Action<CustomEffectEventArgs> OnCustomEffectTriggered;

        private bool _isApplied;
        private GameObject _cachedOwner;
        private int _appliedLevel;

        public override void Apply(GameObject owner, int level)
        {
            if (owner == null || string.IsNullOrEmpty(CallbackId)) return;

            if (_isApplied)
            {
                Remove(owner);
            }

            _isApplied = true;
            _cachedOwner = owner;
            _appliedLevel = level;

            OnCustomEffectTriggered?.Invoke(new CustomEffectEventArgs
            {
                CallbackId = CallbackId,
                Parameter = Parameter,
                Owner = owner,
                Level = level,
                IsApply = true
            });
        }

        public override void Remove(GameObject owner)
        {
            if (!_isApplied) return;

            OnCustomEffectTriggered?.Invoke(new CustomEffectEventArgs
            {
                CallbackId = CallbackId,
                Parameter = Parameter,
                Owner = _cachedOwner,
                Level = _appliedLevel,
                IsApply = false
            });

            _isApplied = false;
            _cachedOwner = null;
            _appliedLevel = 0;
        }

        public override string GetDescription(int level)
        {
            return Description ?? $"自定义效果: {CallbackId}";
        }
    }

    /// <summary>
    /// 自定义效果事件参数
    /// </summary>
    public struct CustomEffectEventArgs
    {
        public string CallbackId;
        public string Parameter;
        public GameObject Owner;
        public int Level;
        public bool IsApply;
    }
}
