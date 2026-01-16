using System;
using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道事件系统
    /// </summary>
    public static class ProjectileEvents
    {
        // 事件常量 (用于 EventManager 集成)
        public const string ProjectileLaunched = "Projectile.Launched";
        public const string ProjectileHit = "Projectile.Hit";
        public const string ProjectileExpired = "Projectile.Expired";
        public const string ProjectileDestroyed = "Projectile.Destroyed";
        public const string ProjectileBounce = "Projectile.Bounce";
        public const string ProjectilePierce = "Projectile.Pierce";

        // 实际事件
        public static event Action<ProjectileLaunchEventArgs> OnProjectileLaunch;
        public static event Action<ProjectileHitEventArgs> OnProjectileHit;
        public static event Action<ProjectileDestroyEventArgs> OnProjectileDestroy;
        public static event Action<ProjectileBounceEventArgs> OnProjectileBounce;

        public static void InvokeLaunch(ProjectileLaunchEventArgs args) => OnProjectileLaunch?.Invoke(args);
        public static void InvokeHit(ProjectileHitEventArgs args) => OnProjectileHit?.Invoke(args);
        public static void InvokeDestroy(ProjectileDestroyEventArgs args) => OnProjectileDestroy?.Invoke(args);
        public static void InvokeBounce(ProjectileBounceEventArgs args) => OnProjectileBounce?.Invoke(args);
    }

    /// <summary>
    /// 弹道发射事件参数
    /// </summary>
    public readonly struct ProjectileLaunchEventArgs
    {
        /// <summary>弹道实例</summary>
        public readonly ProjectileBase Projectile;

        /// <summary>发射位置</summary>
        public readonly Vector3 LaunchPosition;

        /// <summary>发射方向</summary>
        public readonly Vector3 LaunchDirection;

        /// <summary>目标（可选）</summary>
        public readonly Transform Target;

        /// <summary>发射者</summary>
        public readonly Combat.ICombatant Owner;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public ProjectileLaunchEventArgs(
            ProjectileBase projectile,
            Vector3 launchPosition,
            Vector3 launchDirection,
            Transform target,
            Combat.ICombatant owner)
        {
            Projectile = projectile;
            LaunchPosition = launchPosition;
            LaunchDirection = launchDirection;
            Target = target;
            Owner = owner;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 弹道命中事件参数
    /// </summary>
    public readonly struct ProjectileHitEventArgs
    {
        /// <summary>弹道实例</summary>
        public readonly ProjectileBase Projectile;

        /// <summary>命中对象</summary>
        public readonly GameObject HitObject;

        /// <summary>命中点</summary>
        public readonly Vector3 HitPoint;

        /// <summary>命中方向/法线</summary>
        public readonly Vector3 HitNormal;

        /// <summary>命中碰撞体</summary>
        public readonly Collider HitCollider;

        /// <summary>伤害数据</summary>
        public readonly Combat.DamageData? DamageData;

        /// <summary>伤害结果</summary>
        public readonly Combat.DamageResult? DamageResult;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public ProjectileHitEventArgs(
            ProjectileBase projectile,
            GameObject hitObject,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Combat.DamageData? damageData,
            Combat.DamageResult? damageResult)
        {
            Projectile = projectile;
            HitObject = hitObject;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            HitCollider = hitObject != null ? hitObject.GetComponent<Collider>() : null;
            DamageData = damageData;
            DamageResult = damageResult;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 弹道反弹事件参数
    /// </summary>
    public readonly struct ProjectileBounceEventArgs
    {
        /// <summary>弹道实例</summary>
        public readonly ProjectileBase Projectile;

        /// <summary>反弹点</summary>
        public readonly Vector3 BouncePoint;

        /// <summary>反弹法线</summary>
        public readonly Vector3 BounceNormal;

        /// <summary>反弹后方向</summary>
        public readonly Vector3 NewDirection;

        /// <summary>已反弹次数</summary>
        public readonly int BounceCount;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public ProjectileBounceEventArgs(
            ProjectileBase projectile,
            Vector3 bouncePoint,
            Vector3 bounceNormal,
            Vector3 newDirection,
            int bounceCount)
        {
            Projectile = projectile;
            BouncePoint = bouncePoint;
            BounceNormal = bounceNormal;
            NewDirection = newDirection;
            BounceCount = bounceCount;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 弹道销毁事件参数
    /// </summary>
    public readonly struct ProjectileDestroyEventArgs
    {
        /// <summary>弹道实例</summary>
        public readonly ProjectileBase Projectile;

        /// <summary>销毁位置</summary>
        public readonly Vector3 Position;

        /// <summary>销毁原因</summary>
        public readonly ProjectileDestroyReason Reason;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public ProjectileDestroyEventArgs(
            ProjectileBase projectile,
            Vector3 position,
            ProjectileDestroyReason reason)
        {
            Projectile = projectile;
            Position = position;
            Reason = reason;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 弹道销毁原因
    /// </summary>
    public enum ProjectileDestroyReason
    {
        /// <summary>命中目标</summary>
        Hit,
        /// <summary>超时</summary>
        Timeout,
        /// <summary>超出距离</summary>
        MaxDistance,
        /// <summary>目标丢失</summary>
        TargetLost,
        /// <summary>手动销毁</summary>
        Manual,
        /// <summary>穿透次数用尽</summary>
        PierceExhausted,
        /// <summary>反弹次数用尽</summary>
        BounceExhausted,
        /// <summary>轨迹完成</summary>
        TrajectoryComplete
    }
}
