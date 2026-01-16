using UnityEngine;
using UnityEngine.Audio;

namespace ZeroEngine.Audio
{
    /// <summary>
    /// Rich definition for a Sound Effect (SFX).
    /// Supports randomization, spatial settings, and cooldowns.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioCue", menuName = "ZeroEngine/Audio/Audio Cue")]
    public class AudioCueSO : ScriptableObject
    {
        [Header("Clips")]
        [Tooltip("Randomly selects one clip from this array to play.")]
        public AudioClip[] Clips;

        [Header("Settings")]
        [Tooltip("Audio Mixer Group to route this sound to.")]
        public AudioMixerGroup Group;

        [Tooltip("Loop the sound?")]
        public bool Loop = false;

        [Range(0f, 1f)]
        [Tooltip("0 = 2D (UI/BGM), 1 = 3D (World).")]
        public float SpatialBlend = 1f;

        [Header("Randomization")]
        [Tooltip("Random volume range.")]
        public Vector2 VolumeRange = new Vector2(0.9f, 1.0f);

        [Tooltip("Random pitch range.")]
        public Vector2 PitchRange = new Vector2(0.9f, 1.1f);

        [Header("Spam Protection")]
        [Tooltip("Minimum time (seconds) before this cue can be played again.")]
        public float Cooldown = 0.1f;
        
        // --- Helper Methods ---
        
        public AudioClip GetRandomClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            return Clips[Random.Range(0, Clips.Length)];
        }

        public float GetRandomVolume()
        {
            return Random.Range(VolumeRange.x, VolumeRange.y);
        }

        public float GetRandomPitch()
        {
            return Random.Range(PitchRange.x, PitchRange.y);
        }
    }
}
