using UnityEngine;

namespace ZeroEngine.Audio
{
    /// <summary>
    /// Utility component to test Audio System from the Inspector.
    /// Add this to any GameObject to debug audio.
    /// </summary>
    public class AudioDebugger : MonoBehaviour
    {
        [Header("Test SFX")]
        public AudioCueSO SfxCue;
        public AudioClip LegacyClip;

        [Header("Test Music")]
        public AudioMusicSO MusicTrack;
        public float FadeDuration = 1.0f;

        // Methods exposed for Unity Events or Editor Buttons
        
        public void PlaySfxCue()
        {
            if (SfxCue != null)
                AudioManager.Instance.PlaySFX(SfxCue, transform.position);
            else
                Debug.LogWarning("[AudioDebugger] SFX Cue is null!");
        }

        public void PlayLegacyClip()
        {
            if (LegacyClip != null)
                AudioManager.Instance.PlaySFX(LegacyClip);
            else
                Debug.LogWarning("[AudioDebugger] Legacy Clip is null!");
        }

        public void PlayMusic()
        {
            if (MusicTrack != null)
                AudioManager.Instance.PlayMusic(MusicTrack, FadeDuration);
            else
                Debug.LogWarning("[AudioDebugger] Music Track is null!");
        }

        public void StopMusic()
        {
            AudioManager.Instance.StopMusic(FadeDuration);
        }
    }
}
