// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 播放音效事件
// ============================================================================

using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 播放音效事件 - 在指定位置播放音效
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Play Sound")]
#endif
    public class PlaySoundEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("要播放的音效")]
        public AudioClip AudioClip;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("音量")]
        [Range(0f, 1f)]
        public float Volume = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("音调 (1 = 正常)")]
        [Range(0.5f, 2f)]
        public float Pitch = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否使用 3D 空间音效")]
        public bool Is3D = false;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("Is3D")]
#endif
        [Tooltip("3D 音效播放位置")]
        public SpawnTarget PlayAt = SpawnTarget.Caster;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("随机音调变化范围")]
        [Range(0f, 0.5f)]
        public float PitchVariation = 0f;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => PlaySound(context));
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            PlaySound(context);
        }

        private void PlaySound(VisualContext context)
        {
            if (AudioClip == null) return;

            float actualPitch = Pitch;
            if (PitchVariation > 0)
            {
                actualPitch += UnityEngine.Random.Range(-PitchVariation, PitchVariation);
            }

            if (Is3D)
            {
                // 3D 空间音效
                Vector3 position = GetSpawnPosition(context, PlayAt, Vector3.zero);
                PlayClipAtPoint(AudioClip, position, Volume, actualPitch);
            }
            else
            {
                // 2D 音效 (全局)
                PlayClip2D(AudioClip, Volume, actualPitch);
            }
        }

        // ========================================
        // 音频播放辅助方法
        // ========================================

        private static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            // 创建临时 AudioSource
            var tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;

            var audioSource = tempGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.spatialBlend = 1f; // 3D
            audioSource.Play();

            // 播放完毕后销毁
            UnityEngine.Object.Destroy(tempGO, clip.length / pitch);
        }

        private static void PlayClip2D(AudioClip clip, float volume, float pitch)
        {
            // 创建临时 AudioSource
            var tempGO = new GameObject("TempAudio2D");

            var audioSource = tempGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.spatialBlend = 0f; // 2D
            audioSource.Play();

            // 播放完毕后销毁
            UnityEngine.Object.Destroy(tempGO, clip.length / pitch);
        }
    }
}
