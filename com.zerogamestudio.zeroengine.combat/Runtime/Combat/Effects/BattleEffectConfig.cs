using System;
using UnityEngine;

namespace ZeroEngine.Combat.Effects
{
    /// <summary>
    /// 震动效果配置
    /// </summary>
    [Serializable]
    public class ShakeEffectConfig
    {
        [Tooltip("震动强度")]
        public float Strength = 0.15f;

        [Tooltip("持续时间(秒)")]
        public float Duration = 0.25f;

        [Tooltip("震动频率(振动次数)")]
        public int Vibrato = 10;

        [Tooltip("随机性 0-180")]
        [Range(0f, 180f)]
        public float Randomness = 90f;

        [Tooltip("是否渐弱")]
        public bool FadeOut = true;

        public ShakeEffectConfig() { }

        public ShakeEffectConfig(float strength, float duration, int vibrato = 10)
        {
            Strength = strength;
            Duration = duration;
            Vibrato = vibrato;
        }

        /// <summary>
        /// 按倍率缩放强度（用于暴击等强化效果）
        /// </summary>
        public ShakeEffectConfig Scaled(float multiplier)
        {
            return new ShakeEffectConfig
            {
                Strength = Strength * multiplier,
                Duration = Duration,
                Vibrato = Vibrato,
                Randomness = Randomness,
                FadeOut = FadeOut
            };
        }
    }

    /// <summary>
    /// 颜色闪烁效果配置
    /// </summary>
    [Serializable]
    public class FlashEffectConfig
    {
        [Tooltip("闪烁颜色")]
        public Color FlashColor = Color.white;

        [Tooltip("闪烁持续时间(秒)")]
        public float FlashDuration = 0.08f;

        [Tooltip("恢复持续时间(秒)")]
        public float RecoverDuration = 0.15f;

        public FlashEffectConfig() { }

        public FlashEffectConfig(Color color, float flashDuration = 0.08f, float recoverDuration = 0.15f)
        {
            FlashColor = color;
            FlashDuration = flashDuration;
            RecoverDuration = recoverDuration;
        }
    }

    /// <summary>
    /// 缩放脉冲效果配置
    /// </summary>
    [Serializable]
    public class ScalePulseConfig
    {
        [Tooltip("收缩倍率(前摇)")]
        public float ShrinkScale = 0.7f;

        [Tooltip("收缩持续时间")]
        public float ShrinkDuration = 0.12f;

        [Tooltip("爆发倍率")]
        public float BurstScale = 1.4f;

        [Tooltip("爆发持续时间")]
        public float BurstDuration = 0.15f;

        [Tooltip("恢复持续时间")]
        public float RecoverDuration = 0.20f;

        public ScalePulseConfig() { }

        public ScalePulseConfig(float shrink, float burst)
        {
            ShrinkScale = shrink;
            BurstScale = burst;
        }
    }

    /// <summary>
    /// 特效预制体生成配置
    /// </summary>
    [Serializable]
    public class SpawnEffectConfig
    {
        [Tooltip("特效预制体（可为 null 使用运行时粒子）")]
        public GameObject Prefab;

        [Tooltip("特效持续时间")]
        public float Duration = 1.0f;

        [Tooltip("生成偏移")]
        public Vector3 Offset = Vector3.zero;

        [Tooltip("缩放")]
        public Vector3 Scale = Vector3.one;

        [Tooltip("颜色覆盖（用于运行时粒子）")]
        public Color Color = Color.white;

        [Tooltip("粒子数量（运行时粒子模式）")]
        public int ParticleCount = 12;

        public bool HasPrefab => Prefab != null;
    }

    /// <summary>
    /// 击退效果配置
    /// </summary>
    [Serializable]
    public class KnockbackConfig
    {
        [Tooltip("是否启用击退")]
        public bool Enabled = false;

        [Tooltip("击退距离")]
        public float Distance = 0.3f;

        [Tooltip("击退持续时间")]
        public float Duration = 0.15f;

        [Tooltip("回弹持续时间")]
        public float BounceDuration = 0.1f;
    }

    /// <summary>
    /// 受击反馈组合配置 — 组合震动+闪烁+粒子+击退
    /// </summary>
    [Serializable]
    public class HitReactionConfig
    {
        [Tooltip("是否启用")]
        public bool Enabled = true;

        [Header("震动")]
        public ShakeEffectConfig Shake = new ShakeEffectConfig(0.15f, 0.25f);

        [Header("颜色闪烁")]
        public FlashEffectConfig Flash = new FlashEffectConfig(Color.white);

        [Header("命中粒子")]
        public SpawnEffectConfig HitParticle = new SpawnEffectConfig();

        [Header("击退")]
        public KnockbackConfig Knockback = new KnockbackConfig();

        /// <summary>
        /// 创建强化版本（用于暴击）
        /// </summary>
        public HitReactionConfig CreateCriticalVersion(float shakeMultiplier = 2f)
        {
            return new HitReactionConfig
            {
                Enabled = true,
                Shake = Shake.Scaled(shakeMultiplier),
                Flash = new FlashEffectConfig(
                    new Color(1f, 0.9f, 0.3f), // 金色闪烁
                    Flash.FlashDuration * 1.5f,
                    Flash.RecoverDuration * 1.5f),
                HitParticle = new SpawnEffectConfig
                {
                    Prefab = HitParticle.Prefab,
                    Duration = HitParticle.Duration,
                    Offset = HitParticle.Offset,
                    Scale = HitParticle.Scale * 1.5f,
                    Color = new Color(1f, 0.8f, 0.2f),
                    ParticleCount = HitParticle.ParticleCount * 2
                },
                Knockback = new KnockbackConfig
                {
                    Enabled = true,
                    Distance = Knockback.Distance * 1.5f,
                    Duration = Knockback.Duration,
                    BounceDuration = Knockback.BounceDuration
                }
            };
        }

        /// <summary>
        /// 预设：轻微受击（普通攻击）
        /// </summary>
        public static HitReactionConfig Light => new HitReactionConfig
        {
            Shake = new ShakeEffectConfig(0.10f, 0.15f, 8),
            Flash = new FlashEffectConfig(Color.white, 0.06f, 0.10f),
            HitParticle = new SpawnEffectConfig { ParticleCount = 6, Color = Color.white },
            Knockback = new KnockbackConfig { Enabled = false }
        };

        /// <summary>
        /// 预设：标准受击
        /// </summary>
        public static HitReactionConfig Normal => new HitReactionConfig();

        /// <summary>
        /// 预设：重击（暴击/技能）
        /// </summary>
        public static HitReactionConfig Heavy => new HitReactionConfig
        {
            Shake = new ShakeEffectConfig(0.25f, 0.35f, 14),
            Flash = new FlashEffectConfig(new Color(1f, 0.9f, 0.3f), 0.10f, 0.20f),
            HitParticle = new SpawnEffectConfig { ParticleCount = 20, Color = new Color(1f, 0.8f, 0.2f) },
            Knockback = new KnockbackConfig { Enabled = true, Distance = 0.4f }
        };
    }

    /// <summary>
    /// 技能施放特效组合配置
    /// </summary>
    [Serializable]
    public class SkillCastEffectConfig
    {
        [Tooltip("是否启用")]
        public bool Enabled = true;

        [Header("光环颜色")]
        [Tooltip("技能光环/发光颜色")]
        public Color GlowColor = new Color(1f, 0.90f, 0.4f);

        [Header("缩放脉冲")]
        public ScalePulseConfig ScalePulse = new ScalePulseConfig();

        [Header("旋转前摇")]
        [Tooltip("是否启用旋转")]
        public bool EnableRotation = true;

        [Tooltip("旋转角度")]
        public float RotationAngle = 360f;

        [Tooltip("旋转持续时间")]
        public float RotationDuration = 0.3f;

        [Header("施放粒子")]
        public SpawnEffectConfig CastParticle = new SpawnEffectConfig();

        [Header("地面光波")]
        [Tooltip("是否启用光波扩散")]
        public bool EnableGroundWave = true;

        [Tooltip("光波颜色")]
        public Color WaveColor = new Color(1f, 0.87f, 0.2f, 0.6f);

        [Tooltip("光波最大半径")]
        public float WaveMaxScale = 3f;

        [Tooltip("光波持续时间")]
        public float WaveDuration = 0.4f;

        /// <summary>
        /// 预设：火系技能
        /// </summary>
        public static SkillCastEffectConfig Fire => new SkillCastEffectConfig
        {
            GlowColor = new Color(1f, 0.4f, 0.1f),
            CastParticle = new SpawnEffectConfig
            {
                Color = new Color(1f, 0.5f, 0.1f),
                ParticleCount = 16,
                Duration = 0.6f
            },
            WaveColor = new Color(1f, 0.4f, 0.1f, 0.5f)
        };

        /// <summary>
        /// 预设：冰系技能
        /// </summary>
        public static SkillCastEffectConfig Ice => new SkillCastEffectConfig
        {
            GlowColor = new Color(0.5f, 0.8f, 1f),
            CastParticle = new SpawnEffectConfig
            {
                Color = new Color(0.6f, 0.85f, 1f),
                ParticleCount = 14,
                Duration = 0.7f
            },
            WaveColor = new Color(0.5f, 0.8f, 1f, 0.5f)
        };

        /// <summary>
        /// 预设：治疗技能
        /// </summary>
        public static SkillCastEffectConfig Heal => new SkillCastEffectConfig
        {
            GlowColor = new Color(0.3f, 1f, 0.5f),
            EnableRotation = false,
            ScalePulse = new ScalePulseConfig(0.85f, 1.2f),
            CastParticle = new SpawnEffectConfig
            {
                Color = new Color(0.4f, 1f, 0.6f),
                ParticleCount = 10,
                Duration = 0.8f
            },
            WaveColor = new Color(0.3f, 1f, 0.5f, 0.4f)
        };

        /// <summary>
        /// 预设：物理技能（重击等）
        /// </summary>
        public static SkillCastEffectConfig Physical => new SkillCastEffectConfig
        {
            GlowColor = new Color(1f, 0.7f, 0.3f),
            EnableRotation = false,
            ScalePulse = new ScalePulseConfig(0.6f, 1.5f),
            CastParticle = new SpawnEffectConfig
            {
                Color = new Color(1f, 0.8f, 0.4f),
                ParticleCount = 8,
                Duration = 0.4f
            },
            EnableGroundWave = false
        };

        /// <summary>
        /// 预设：暗影/诅咒技能
        /// </summary>
        public static SkillCastEffectConfig Dark => new SkillCastEffectConfig
        {
            GlowColor = new Color(0.6f, 0.2f, 0.8f),
            CastParticle = new SpawnEffectConfig
            {
                Color = new Color(0.5f, 0.1f, 0.7f),
                ParticleCount = 14,
                Duration = 0.7f
            },
            WaveColor = new Color(0.5f, 0.15f, 0.7f, 0.5f)
        };

        /// <summary>
        /// 预设：默认/通用
        /// </summary>
        public static SkillCastEffectConfig Default => new SkillCastEffectConfig();
    }
}
