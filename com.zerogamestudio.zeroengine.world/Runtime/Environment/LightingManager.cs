using System;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.EnvironmentSystem
{
    /// <summary>
    /// 光照控制器
    /// 根据 TimeManager 时间控制场景光照、太阳方向、环境光、雾效
    /// </summary>
    public class LightingManager : MonoSingleton<LightingManager>
    {
        [Header("References")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private DayNightPresetSO _preset;

        [Header("Settings")]
        [SerializeField] private bool _useSmoothTransition = true;
        [SerializeField, Range(0.1f, 2f)] private float _smoothTime = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode;

        // 内部状态
        private bool _isSubscribed;

        // 平滑过渡目标值
        private Color _targetSunColor;
        private float _targetIntensity;
        private Quaternion _targetRotation;
        private Color _targetAmbientColor;
        private Color _targetFogColor;

        // 当前值 (用于平滑过渡)
        private Color _currentSunColor;
        private float _currentIntensity;
        private Quaternion _currentRotation;
        private Color _currentAmbientColor;
        private Color _currentFogColor;

        #region Properties

        public DayNightPresetSO Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                if (_preset != null && TimeManager.Instance != null)
                    ApplyLightingImmediate(TimeManager.Instance.NormalizedTime);
            }
        }

        public Light SunLight
        {
            get => _sunLight;
            set => _sunLight = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            TrySubscribe();

            if (TimeManager.Instance == null || _preset == null || _sunLight == null) return;

            float normalizedTime = TimeManager.Instance.NormalizedTime;

            // 计算目标值
            CalculateTargetValues(normalizedTime);

            // 应用值 (平滑或直接)
            if (_useSmoothTransition)
            {
                ApplySmoothed();
            }
            else
            {
                ApplyImmediate();
            }
        }

        private void OnDisable()
        {
            _isSubscribed = false;
        }

        #endregion

        #region Public API

        /// <summary>强制立即刷新光照</summary>
        public void ForceRefresh()
        {
            if (TimeManager.Instance != null && _preset != null)
            {
                ApplyLightingImmediate(TimeManager.Instance.NormalizedTime);
            }
        }

        /// <summary>设置平滑过渡</summary>
        public void SetSmoothTransition(bool enabled, float smoothTime = 0.5f)
        {
            _useSmoothTransition = enabled;
            _smoothTime = smoothTime;
        }

        #endregion

        #region Internal

        private void TrySubscribe()
        {
            if (_isSubscribed) return;

            if (TimeManager.Instance != null && _preset != null && _sunLight != null)
            {
                _isSubscribed = true;
                ApplyLightingImmediate(TimeManager.Instance.NormalizedTime);
            }
        }

        private void CalculateTargetValues(float normalizedTime)
        {
            _targetSunColor = _preset.SunColorOverDay.Evaluate(normalizedTime);
            _targetIntensity = _preset.SunIntensityOverDay.Evaluate(normalizedTime) * _preset.MaxSunIntensity;
            _targetAmbientColor = _preset.AmbientColorOverDay.Evaluate(normalizedTime);
            _targetRotation = CalculateSunRotation(normalizedTime);

            if (_preset.EnableFog)
            {
                _targetFogColor = _preset.FogColorOverDay.Evaluate(normalizedTime);
            }
        }

        private void ApplySmoothed()
        {
            float t = Time.deltaTime / _smoothTime;

            // Sun
            _currentSunColor = Color.Lerp(_currentSunColor, _targetSunColor, t);
            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, t);
            _currentRotation = Quaternion.Slerp(_currentRotation, _targetRotation, t);
            _currentAmbientColor = Color.Lerp(_currentAmbientColor, _targetAmbientColor, t);

            _sunLight.color = _currentSunColor;
            _sunLight.intensity = _currentIntensity;
            _sunLight.transform.rotation = _currentRotation;
            RenderSettings.ambientLight = _currentAmbientColor;

            if (_preset.EnableFog)
            {
                RenderSettings.fog = true;
                _currentFogColor = Color.Lerp(_currentFogColor, _targetFogColor, t);
                RenderSettings.fogColor = _currentFogColor;
            }
        }

        private void ApplyImmediate()
        {
            _sunLight.color = _targetSunColor;
            _sunLight.intensity = _targetIntensity;
            _sunLight.transform.rotation = _targetRotation;
            RenderSettings.ambientLight = _targetAmbientColor;

            if (_preset.EnableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = _targetFogColor;
            }

            // 同步当前值
            _currentSunColor = _targetSunColor;
            _currentIntensity = _targetIntensity;
            _currentRotation = _targetRotation;
            _currentAmbientColor = _targetAmbientColor;
            _currentFogColor = _targetFogColor;
        }

        private void ApplyLightingImmediate(float normalizedTime)
        {
            CalculateTargetValues(normalizedTime);
            ApplyImmediate();
            Log($"光照初始化: Time={normalizedTime:F2}");
        }

        private Quaternion CalculateSunRotation(float normalizedTime)
        {
            // normalizedTime: 0 = 午夜, 0.25 = 6:00, 0.5 = 正午, 0.75 = 18:00
            const float sunriseTime = 0.25f;
            const float sunsetTime = 0.75f;
            const float transitionDuration = 0.04f;

            const float minElevation = 5f;
            const float horizonElevation = 15f;
            const float maxElevation = 75f;

            float sunElevation;

            // 日出过渡区
            if (normalizedTime >= sunriseTime - transitionDuration && normalizedTime < sunriseTime + transitionDuration)
            {
                float t = (normalizedTime - (sunriseTime - transitionDuration)) / (transitionDuration * 2f);
                sunElevation = Mathf.Lerp(minElevation, horizonElevation, Mathf.SmoothStep(0f, 1f, t));
            }
            // 日落过渡区
            else if (normalizedTime >= sunsetTime - transitionDuration && normalizedTime < sunsetTime + transitionDuration)
            {
                float t = (normalizedTime - (sunsetTime - transitionDuration)) / (transitionDuration * 2f);
                sunElevation = Mathf.Lerp(horizonElevation, minElevation, Mathf.SmoothStep(0f, 1f, t));
            }
            // 白天
            else if (normalizedTime >= sunriseTime + transitionDuration && normalizedTime <= sunsetTime - transitionDuration)
            {
                float dayStart = sunriseTime + transitionDuration;
                float dayEnd = sunsetTime - transitionDuration;
                float dayProgress = (normalizedTime - dayStart) / (dayEnd - dayStart);
                sunElevation = horizonElevation + (maxElevation - horizonElevation) * Mathf.Sin(dayProgress * Mathf.PI);
            }
            // 夜间
            else
            {
                sunElevation = minElevation;
            }

            float sunAzimuth = Mathf.Lerp(_preset.SunriseAngle, _preset.SunsetAngle, normalizedTime);

            return Quaternion.Euler(sunElevation, sunAzimuth, 0f);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode) Debug.Log($"[Lighting] {message}");
        }

        #endregion
    }
}
