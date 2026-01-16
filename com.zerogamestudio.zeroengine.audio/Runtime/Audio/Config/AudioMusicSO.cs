using UnityEngine;

namespace ZeroEngine.Audio
{
    /// <summary>
    /// Definition for a Music Track, supporting Intro + Loop structure.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioMusic", menuName = "ZeroEngine/Audio/Audio Music")]
    public class AudioMusicSO : ScriptableObject
    {
        [Tooltip("Played once at the beginning.")]
        public AudioClip IntroClip;

        [Tooltip("Looped indefinitely after Intro finishes (or immediately if Intro is null).")]
        public AudioClip LoopClip;

        [Range(0f, 1f)]
        public float Volume = 1f;
    }
}
