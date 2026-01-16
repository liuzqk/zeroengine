using System;
using System.Collections;
using UnityEngine;
using ZeroEngine.Pool;

namespace ZeroEngine.Audio
{
    /// <summary>
    /// A pooled object that handles playing an AudioCue.
    /// Manages the AudioSource lifecycle and auto-returns to pool.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioEmitter : MonoBehaviour, IPoolable
    {
        private AudioSource _source;
        private Coroutine _playingRoutine;
        private Action<AudioEmitter> _onFinishCallback;

        public AudioSource Source
        {
            get
            {
                if (_source == null) _source = GetComponent<AudioSource>();
                return _source;
            }
        }

        public bool IsPlaying => Source.isPlaying;

        public void Initialize(Action<AudioEmitter> onFinishCallback)
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _onFinishCallback = onFinishCallback;
        }

        public void OnSpawn()
        {
            // Reset for fresh use if needed
        }

        public void OnDespawn()
        {
            if (_playingRoutine != null) StopCoroutine(_playingRoutine);
            _playingRoutine = null;
            
            if (_source != null)
            {
                _source.Stop();
                _source.clip = null;
            }
        }

        public void Play(AudioCueSO cue)
        {
            if (_playingRoutine != null) StopCoroutine(_playingRoutine);

            AudioClip clip = cue.GetRandomClip();
            if (clip == null)
            {
                HandleFinish();
                return;
            }

            // Apply Settings
            Source.clip = clip; // Ensure Source property is used
            _source.outputAudioMixerGroup = cue.Group; 
            _source.volume = cue.GetRandomVolume();
            _source.pitch = cue.GetRandomPitch();
            _source.loop = cue.Loop;
            _source.spatialBlend = cue.SpatialBlend;

            _source.Play();

            if (!cue.Loop)
            {
                // Schedule return
                _playingRoutine = StartCoroutine(WaitForFinish(clip.length / Mathf.Abs(_source.pitch)));
            }
        }

        public void Stop()
        {
            HandleFinish();
        }

        private IEnumerator WaitForFinish(float duration)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) yield break;
#endif
            yield return new WaitForSeconds(duration + 0.1f);
            HandleFinish();
        }

        private void HandleFinish()
        {
            // AudioManager will call OnDespawn when handling the callback logic
            // or we call it here?
            // Since AudioManager manages the pool, it should call OnDespawn when putting back.
            // But we trigger it.
            _onFinishCallback?.Invoke(this);
        }
    }
}
