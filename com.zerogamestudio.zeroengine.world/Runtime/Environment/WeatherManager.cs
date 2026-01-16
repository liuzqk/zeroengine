using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.EnvironmentSystem
{
    /// <summary>
    /// 天气系统管理器
    /// 管理天气效果、VFX、雾效、环境音效
    /// </summary>
    public class WeatherManager : MonoSingleton<WeatherManager>, ISaveable
    {
        [Header("Current Weather")]
        [SerializeField] private WeatherPresetSO _currentWeather;

        [Header("Available Presets")]
        [SerializeField] private List<WeatherPresetSO> _weatherPresets = new List<WeatherPresetSO>();

        [Header("References")]
        [SerializeField] private Transform _followTarget;

        [Header("Debug")]
        [SerializeField] private bool _debugMode;

        // 事件
        public event Action<EnvironmentEventArgs> OnEnvironmentEvent;

        // 运行时
        private GameObject _activeVfx;
        private AudioSource _currentAmbientSource;
        private float _originalFogDensity;
        private Color _originalFogColor;
        private bool _originalFogEnabled;

        // 查找缓存
        private readonly Dictionary<WeatherType, WeatherPresetSO> _presetLookup = new Dictionary<WeatherType, WeatherPresetSO>();

        #region Properties

        public WeatherPresetSO CurrentWeather => _currentWeather;
        public WeatherType CurrentWeatherType => _currentWeather != null ? _currentWeather.WeatherType : WeatherType.Clear;

        #endregion

        #region ISaveable

        public string SaveKey => "WeatherManager";

        public void Register() => SaveSlotManager.Instance?.Register(this);
        public void Unregister() => SaveSlotManager.Instance?.Unregister(this);

        public object ExportSaveData()
        {
            return new WeatherSaveData
            {
                CurrentWeatherType = CurrentWeatherType
            };
        }

        public void ImportSaveData(object data)
        {
            if (data is not WeatherSaveData saveData) return;
            SetWeather(saveData.CurrentWeatherType);
        }

        public void ResetToDefault()
        {
            ClearWeather();
            if (_weatherPresets.Count > 0)
                SetWeather(_weatherPresets[0]);
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // 保存原始雾效设置
            _originalFogEnabled = RenderSettings.fog;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;

            BuildPresetLookup();
        }

        private void Start()
        {
            Register();

            if (_followTarget == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null) _followTarget = mainCam.transform;
            }

            // 应用初始天气
            if (_currentWeather != null)
            {
                ApplyWeather(_currentWeather, true);
            }
        }

        private void LateUpdate()
        {
            // VFX 跟随相机
            if (_activeVfx != null && _followTarget != null && _currentWeather != null)
            {
                _activeVfx.transform.position = _followTarget.position + _currentWeather.VfxOffset;
            }
        }

        protected override void OnDestroy()
        {
            Unregister();
            base.OnDestroy();
        }

        #endregion

        #region Public API

        /// <summary>设置天气</summary>
        public void SetWeather(WeatherPresetSO preset)
        {
            if (preset == null || preset == _currentWeather) return;

            var previousType = CurrentWeatherType;
            _currentWeather = preset;
            ApplyWeather(preset, false);

            OnEnvironmentEvent?.Invoke(EnvironmentEventArgs.WeatherChanged(preset.WeatherType, previousType));
            Log($"天气变更: {previousType} -> {preset.WeatherType}");
        }

        /// <summary>通过类型设置天气</summary>
        public void SetWeather(WeatherType type)
        {
            var preset = GetPreset(type);
            if (preset != null)
            {
                SetWeather(preset);
            }
        }

        /// <summary>获取预设</summary>
        public WeatherPresetSO GetPreset(WeatherType type)
        {
            _presetLookup.TryGetValue(type, out var preset);
            return preset;
        }

        /// <summary>清除天气效果</summary>
        public void ClearWeather()
        {
            // 移除 VFX
            if (_activeVfx != null)
            {
                Destroy(_activeVfx);
                _activeVfx = null;
            }

            // 停止环境音效
            StopAmbientSound();

            // 恢复雾效
            RestoreFog();

            _currentWeather = null;
        }

        /// <summary>设置跟随目标</summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        /// <summary>注册新的天气预设</summary>
        public void RegisterPreset(WeatherPresetSO preset)
        {
            if (preset == null) return;
            if (!_weatherPresets.Contains(preset))
                _weatherPresets.Add(preset);
            _presetLookup[preset.WeatherType] = preset;
        }

        #endregion

        #region Internal

        private void BuildPresetLookup()
        {
            _presetLookup.Clear();
            foreach (var preset in _weatherPresets)
            {
                if (preset != null)
                    _presetLookup[preset.WeatherType] = preset;
            }
        }

        private void ApplyWeather(WeatherPresetSO preset, bool immediate)
        {
            float duration = immediate ? 0f : preset.TransitionDuration;

            // 1. VFX
            if (_activeVfx != null)
            {
                Destroy(_activeVfx);
                _activeVfx = null;
            }

            if (preset.VfxPrefab != null && _followTarget != null)
            {
                _activeVfx = Instantiate(preset.VfxPrefab,
                    _followTarget.position + preset.VfxOffset,
                    Quaternion.identity);
            }

            // 2. Fog
            if (preset.OverrideFog)
            {
                RenderSettings.fog = preset.EnableFog;
                if (preset.EnableFog)
                {
                    if (immediate)
                    {
                        RenderSettings.fogColor = preset.FogColor;
                        RenderSettings.fogDensity = preset.FogDensity;
                    }
                    else
                    {
                        StartCoroutine(TransitionFog(preset.FogColor, preset.FogDensity, duration));
                    }
                }
            }

            // 3. Audio
            PlayAmbientSound(preset, duration);
        }

        private System.Collections.IEnumerator TransitionFog(Color targetColor, float targetDensity, float duration)
        {
            Color startColor = RenderSettings.fogColor;
            float startDensity = RenderSettings.fogDensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                RenderSettings.fogColor = Color.Lerp(startColor, targetColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, t);

                yield return null;
            }

            RenderSettings.fogColor = targetColor;
            RenderSettings.fogDensity = targetDensity;
        }

        private void RestoreFog()
        {
            StartCoroutine(TransitionFog(_originalFogColor, _originalFogDensity, 1f));
        }

        private void PlayAmbientSound(WeatherPresetSO preset, float fadeDuration)
        {
            StopAmbientSound();

            if (preset.AmbientSound == null) return;

            // 创建 AudioSource 播放环境音
            var go = new GameObject("WeatherAmbient");
            go.transform.SetParent(transform);
            _currentAmbientSource = go.AddComponent<AudioSource>();
            _currentAmbientSource.clip = preset.AmbientSound;
            _currentAmbientSource.loop = true;
            _currentAmbientSource.volume = 0f;
            _currentAmbientSource.Play();

            StartCoroutine(FadeAudioVolume(_currentAmbientSource, preset.AmbientVolume, fadeDuration));
        }

        private void StopAmbientSound()
        {
            if (_currentAmbientSource != null)
            {
                StartCoroutine(FadeOutAndDestroy(_currentAmbientSource, 1f));
                _currentAmbientSource = null;
            }
        }

        private System.Collections.IEnumerator FadeAudioVolume(AudioSource source, float targetVolume, float duration)
        {
            if (source == null) yield break;

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration && source != null)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            if (source != null)
                source.volume = targetVolume;
        }

        private System.Collections.IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
        {
            if (source == null) yield break;

            yield return FadeAudioVolume(source, 0f, duration);

            if (source != null)
                Destroy(source.gameObject);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode) Debug.Log($"[Weather] {message}");
        }

        #endregion
    }

    #region Save Data

    [Serializable]
    public class WeatherSaveData
    {
        public WeatherType CurrentWeatherType;
    }

    #endregion
}
