using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道配置数据
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileData", menuName = "ZeroEngine/Projectile/Projectile Data")]
    public class ProjectileDataSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("弹道ID")]
        public string ProjectileId;

        [Tooltip("显示名称")]
        public string DisplayName;

        [Tooltip("弹道预制体")]
        public GameObject Prefab;

        [Header("运动属性")]
        [Tooltip("移动速度")]
        public float Speed = 10f;

        [Tooltip("最大飞行时间")]
        public float MaxLifetime = 5f;

        [Tooltip("最大飞行距离（0表示无限）")]
        public float MaxDistance = 0f;

        [Tooltip("轨迹类型")]
        public TrajectoryType TrajectoryType = TrajectoryType.Linear;

        [Header("追踪属性（仅追踪弹道）")]
        [Tooltip("追踪转向速度（度/秒）")]
        public float HomingTurnSpeed = 180f;

        [Tooltip("追踪延迟时间")]
        public float HomingDelay = 0f;

        [Tooltip("失去目标后继续飞行")]
        public bool ContinueWithoutTarget = true;

        [Header("抛物线属性")]
        [Tooltip("重力系数")]
        public float Gravity = 9.8f;

        [Tooltip("初始仰角（度）")]
        public float LaunchAngle = 45f;

        [Header("贝塞尔曲线属性")]
        [Tooltip("控制点偏移")]
        public Vector3 ControlPointOffset = new Vector3(0, 5f, 0);

        [Tooltip("曲线高度随机范围")]
        public float CurveHeightVariance = 2f;

        [Header("碰撞属性")]
        [Tooltip("碰撞检测半径")]
        public float CollisionRadius = 0.1f;

        [Tooltip("碰撞层级")]
        public LayerMask CollisionMask = ~0;

        [Tooltip("穿透次数（0表示不穿透）")]
        public int PierceCount = 0;

        [Tooltip("反弹次数（0表示不反弹）")]
        public int BounceCount = 0;

        [Header("伤害属性")]
        [Tooltip("基础伤害")]
        public float BaseDamage = 10f;

        [Tooltip("伤害类型")]
        public Combat.DamageType DamageType = Combat.DamageType.Physical;

        [Tooltip("暴击率加成")]
        public float CritChanceBonus = 0f;

        [Tooltip("暴击伤害加成")]
        public float CritDamageBonus = 0f;

        [Header("AOE属性")]
        [Tooltip("是否AOE伤害")]
        public bool IsAOE = false;

        [Tooltip("AOE半径")]
        public float AOERadius = 3f;

        [Tooltip("AOE伤害衰减（按距离）")]
        public AnimationCurve AOEDamageFalloff = AnimationCurve.Linear(0, 1, 1, 0.5f);

        [Header("视觉效果")]
        [Tooltip("命中特效预制体")]
        public GameObject HitEffectPrefab;

        [Tooltip("拖尾特效预制体")]
        public GameObject TrailEffectPrefab;

        [Tooltip("弹道缩放")]
        public float Scale = 1f;

        [Header("音效")]
        [Tooltip("发射音效")]
        public AudioClip LaunchSound;

        [Tooltip("飞行音效（循环）")]
        public AudioClip FlightSound;

        [Tooltip("命中音效")]
        public AudioClip HitSound;

        [Header("对象池")]
        [Tooltip("是否使用对象池")]
        public bool UsePooling = true;

        [Tooltip("预热数量")]
        public int PoolPrewarmCount = 10;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ProjectileId))
            {
                ProjectileId = name;
            }
            Speed = Mathf.Max(0.1f, Speed);
            MaxLifetime = Mathf.Max(0.1f, MaxLifetime);
            CollisionRadius = Mathf.Max(0.01f, CollisionRadius);
            AOERadius = Mathf.Max(0.1f, AOERadius);
        }
#endif
    }

    /// <summary>
    /// 轨迹类型
    /// </summary>
    public enum TrajectoryType
    {
        /// <summary>直线</summary>
        Linear,
        /// <summary>抛物线</summary>
        Parabolic,
        /// <summary>追踪</summary>
        Homing,
        /// <summary>贝塞尔曲线</summary>
        Bezier,
        /// <summary>自定义</summary>
        Custom
    }
}
