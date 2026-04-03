using UnityEngine;

namespace ZeroEngine.Combat.Effects
{
    /// <summary>
    /// 战斗效果播放器接口 — 定义通用的战斗特效播放能力
    /// 项目层（如 P6）实现此接口，使用具体的动画库（DOTween 等）
    /// </summary>
    public interface IBattleEffectPlayer
    {
        /// <summary>
        /// 播放受击反馈效果
        /// </summary>
        /// <param name="target">受击目标 Transform</param>
        /// <param name="renderer">目标 Renderer（用于颜色闪烁）</param>
        /// <param name="config">受击配置</param>
        /// <param name="hitDirection">伤害来源方向（用于击退和粒子方向）</param>
        void PlayHitReaction(Transform target, Renderer renderer, HitReactionConfig config, Vector3 hitDirection = default);

        /// <summary>
        /// 播放技能施放特效
        /// </summary>
        /// <param name="caster">施法者 Transform</param>
        /// <param name="renderer">施法者 Renderer</param>
        /// <param name="baseColor">施法者基础颜色（用于恢复）</param>
        /// <param name="config">施放配置</param>
        void PlaySkillCast(Transform caster, Renderer renderer, Color baseColor, SkillCastEffectConfig config);

        /// <summary>
        /// 播放震动效果
        /// </summary>
        void PlayShake(Transform target, ShakeEffectConfig config);

        /// <summary>
        /// 播放颜色闪烁效果
        /// </summary>
        void PlayFlash(Renderer renderer, Color baseColor, FlashEffectConfig config);

        /// <summary>
        /// 播放缩放脉冲效果
        /// </summary>
        void PlayScalePulse(Transform target, Vector3 baseScale, ScalePulseConfig config);

        /// <summary>
        /// 在指定位置生成特效
        /// </summary>
        void SpawnEffect(Vector3 position, SpawnEffectConfig config, Quaternion rotation = default);
    }
}
