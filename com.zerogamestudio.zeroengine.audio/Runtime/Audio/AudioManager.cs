using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.Audio
{
    /// <summary>
    /// Standard Audio Manager for ZeroEngine.
    /// Manages Background Music (BGM) and Sound Effects (SFX).
    /// Supports AudioMixer for volume control.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Mixer Settings")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _bgmGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Header("BGM")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private float _crossFadeTime = 1.0f;

        [Header("SFX Pooling")]
        [SerializeField] private int _initialPoolSize = 10;
        private Queue<AudioEmitter> _sfxPool = new Queue<AudioEmitter>();
        private List<AudioEmitter> _activeEmitters = new List<AudioEmitter>();
        private GameObject _poolRoot;

        // Cooldown Tracking
        private Dictionary<AudioCueSO, float> _cueCooldowns = new Dictionary<AudioCueSO, float>();

        // Music State
        private Coroutine _bgmRoutine;
        private AudioMusicSO _currentMusic;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            InitializePool();
            InitializeBGM();
            InitializeVolume();
        }

        private void Update()
        {
            // Update Cooldowns
            if (_cueCooldowns.Count > 0)
            {
                var keys = new List<AudioCueSO>(_cueCooldowns.Keys);
                foreach (var key in keys)
                {
                    _cueCooldowns[key] -= Time.deltaTime;
                    if (_cueCooldowns[key] <= 0f)
                    {
                        _cueCooldowns.Remove(key);
                    }
                }
            }
        }

        #region SFX System

        private void InitializePool()
        {
            _poolRoot = new GameObject("SFX_Pool");
            _poolRoot.transform.SetParent(transform);

            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewEmitter();
            }
        }

        private AudioEmitter CreateNewEmitter()
        {
            GameObject obj = new GameObject($"SFX_Emitter_{_sfxPool.Count + _activeEmitters.Count}");
            obj.transform.SetParent(_poolRoot.transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _sfxGroup;

            AudioEmitter emitter = obj.AddComponent<AudioEmitter>();
            emitter.Initialize(OnEmitterFinished);
            
            obj.SetActive(false);
            _sfxPool.Enqueue(emitter);
            
            return emitter;
        }

        private void OnEmitterFinished(AudioEmitter emitter)
        {
            if (_activeEmitters.Contains(emitter))
            {
                _activeEmitters.Remove(emitter);
                
                // Call OnDespawn interface
                emitter.OnDespawn();
                
                emitter.gameObject.SetActive(false);
                emitter.transform.SetParent(_poolRoot.transform);
                _sfxPool.Enqueue(emitter);
            }
        }

        /// <summary>
        /// Play an AudioCue at a specific position.
        /// </summary>
        public void PlaySFX(AudioCueSO cue, Vector3 position = default)
        {
            if (cue == null) return;

            // Check Cooldown
            if (cue.Cooldown > 0f && _cueCooldowns.ContainsKey(cue))
            {
                return;
            }

            // Apply Cooldown
            if (cue.Cooldown > 0f)
            {
                _cueCooldowns[cue] = cue.Cooldown;
            }

            // Get Emitter
            AudioEmitter emitter = _sfxPool.Count > 0 ? _sfxPool.Dequeue() : CreateNewEmitter();
            
            // Call OnSpawn interface
            emitter.OnSpawn();
            
            // Keep parented to root to avoid hierarchy clutter
            // emitter.transform.SetParent(null); // REMOVED
            
            if (position != default)
            {
                emitter.transform.position = position;
            }
            else
            {
                // If 2D sound, attach to camera or keep local zero?
                // Just keep it at root (0,0,0) or rely on SpatialBlend=0 ignoring position.
                emitter.transform.localPosition = Vector3.zero;
            }

            emitter.gameObject.SetActive(true);
            _activeEmitters.Add(emitter);
            
            // Override Mixer Group if Cue has one, else use default SFX Group
            if (cue.Group == null) emitter.Source.outputAudioMixerGroup = _sfxGroup;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Editor Mode: Just play, don't use Emitter.Play which starts Coroutine
                // Actually Emitter.Play starts coroutine for auto-return.
                // We can't run coroutine.
                // We'll manually set clips and play.
                AudioClip clip = cue.GetRandomClip();
                if (clip != null)
                {
                    emitter.Source.clip = clip;
                    emitter.Source.volume = cue.GetRandomVolume();
                    emitter.Source.pitch = cue.GetRandomPitch();
                    emitter.Source.spatialBlend = cue.SpatialBlend;
                    emitter.Source.Play();
                    // We can't auto-recycle in Editor easily without EditorUpdate.
                    // It's just a preview, let it sit there.
                }
                return;
            }
#endif

            emitter.Play(cue);
        }

        /// <summary>
        /// Legacy helper for simple clips (wraps internally).
        /// Note: No cooldown support here.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            
            AudioEmitter emitter = _sfxPool.Count > 0 ? _sfxPool.Dequeue() : CreateNewEmitter();
            emitter.gameObject.SetActive(true);
            _activeEmitters.Add(emitter);
            
            emitter.Source.clip = clip;
            emitter.Source.volume = volume;
            emitter.Source.pitch = 1f;
            emitter.Source.loop = false;
            emitter.Source.spatialBlend = 0f;
            emitter.Source.outputAudioMixerGroup = _sfxGroup;
            emitter.Source.Play();
            
#if UNITY_EDITOR
            if (!Application.isPlaying) return; // No coroutine in editor
#endif
            StartCoroutine(ReturnEmitterDelayed(emitter, clip.length + 0.1f));
        }

        private IEnumerator ReturnEmitterDelayed(AudioEmitter emitter, float delay)
        {
            yield return new WaitForSeconds(delay);
            OnEmitterFinished(emitter);
        }

        #endregion

        #region BGM System

        private void InitializeBGM()
        {
            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.outputAudioMixerGroup = _bgmGroup;
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
            }
        }

        public void PlayMusic(AudioMusicSO music, float fadeDuration = -1.0f)
        {
            if (music == null) return;
            if (_currentMusic == music) return; // Already playing

            if (fadeDuration < 0f) fadeDuration = _crossFadeTime;

            _currentMusic = music;
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_bgmSource == null) InitializeBGM();
                // Editor mode: Direct switch, no fade coroutines
                if (_bgmRoutine != null) { StopCoroutine(_bgmRoutine); _bgmRoutine = null; }
                
                _bgmSource.Stop();
                if (music.IntroClip != null)
                {
                    // Can't easily sequence Intro->Loop in Editor without update loop
                    // Just play Loop for preview, or Intro? Let's play Loop.
                    _bgmSource.clip = music.LoopClip != null ? music.LoopClip : music.IntroClip;
                    _bgmSource.loop = true;
                }
                else
                {
                    _bgmSource.clip = music.LoopClip;
                    _bgmSource.loop = true;
                }
                
                _bgmSource.volume = music.Volume;
                _bgmSource.Play();
                return;
            }
#endif

            if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);
            _bgmRoutine = StartCoroutine(PlayMusicRoutine(music, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_bgmSource != null) _bgmSource.Stop();
                _currentMusic = null;
                return;
            }
#endif
            if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);
            _bgmRoutine = StartCoroutine(FadeOutBGM(fadeDuration));
            _currentMusic = null;
        }

        private IEnumerator PlayMusicRoutine(AudioMusicSO music, float fadeDuration)
        {
            // 1. Fade Out current if playing
            if (_bgmSource.isPlaying)
            {
                yield return FadeOutBGM(fadeDuration * 0.5f);
            }

            // 2. Setup new
            _bgmSource.volume = 0f;
            float targetVolume = music.Volume;

            // 3. Intro?
            if (music.IntroClip != null)
            {
                _bgmSource.clip = music.IntroClip;
                _bgmSource.loop = false;
                _bgmSource.Play();
                
                // Fade In
                yield return FadeInBGM(targetVolume, fadeDuration * 0.5f);

                // Wait for Intro
                yield return new WaitForSeconds(music.IntroClip.length - _bgmSource.time); // Wait remainder
                
                // Switch to Loop
                _bgmSource.clip = music.LoopClip;
                _bgmSource.loop = true;
                _bgmSource.Play();
            }
            else
            {
                // Just Loop
                _bgmSource.clip = music.LoopClip;
                _bgmSource.loop = true;
                _bgmSource.Play();
                yield return FadeInBGM(targetVolume, fadeDuration * 0.5f);
            }
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVol = _bgmSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            _bgmSource.Stop();
        }

        private IEnumerator FadeInBGM(float targetVol, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVol, t / duration);
                yield return null;
            }
            _bgmSource.volume = targetVol;
        }

        #endregion

        #region Volume Control

        private const string VOL_MASTER = "MasterVolume";
        private const string VOL_BGM = "BGMVolume";
        private const string VOL_SFX = "SFXVolume";

        private void InitializeVolume()
        {
            if (_audioMixer == null) return;
            
            // Load saved volumes (0-1), default to 1.0f
            float master = SaveManager.Instance.Load(VOL_MASTER, 1.0f, SaveManager.SettingsFile);
            float bgm = SaveManager.Instance.Load(VOL_BGM, 1.0f, SaveManager.SettingsFile);
            float sfx = SaveManager.Instance.Load(VOL_SFX, 1.0f, SaveManager.SettingsFile);
            
            SetMasterVolume(master);
            SetBGMVolume(bgm);
            SetSFXVolume(sfx);
        }

        public void SetMasterVolume(float volume)
        {
            SetMixerVolume(VOL_MASTER, volume);
            SaveManager.Instance.Save(VOL_MASTER, volume, SaveManager.SettingsFile);
        }

        public void SetBGMVolume(float volume)
        {
            SetMixerVolume(VOL_BGM, volume);
            SaveManager.Instance.Save(VOL_BGM, volume, SaveManager.SettingsFile);
        }

        public void SetSFXVolume(float volume)
        {
            SetMixerVolume(VOL_SFX, volume);
            SaveManager.Instance.Save(VOL_SFX, volume, SaveManager.SettingsFile);
        }

        public float GetMasterVolume() => SaveManager.Instance.Load(VOL_MASTER, 1.0f, SaveManager.SettingsFile);
        public float GetBGMVolume() => SaveManager.Instance.Load(VOL_BGM, 1.0f, SaveManager.SettingsFile);
        public float GetSFXVolume() => SaveManager.Instance.Load(VOL_SFX, 1.0f, SaveManager.SettingsFile);

        private void SetMixerVolume(string parameter, float volume)
        {
            if (_audioMixer)
            {
                // Convert 0-1 to Decibel
                // Log10(0) is undefined, so clamp min volume.
                // -80dB is effective silence in AudioMixer.
                float db = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20f;
                _audioMixer.SetFloat(parameter, db);
            }
        }

        #endregion
    }
}
