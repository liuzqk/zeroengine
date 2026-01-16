// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 生成特效事件
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
    /// 生成特效事件 - 在指定位置生成 VFX Prefab
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Spawn VFX")]
#endif
    public class SpawnVFXEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("要生成的特效 Prefab")]
        public GameObject VFXPrefab;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("特效生成在谁身上")]
        public SpawnTarget SpawnOn = SpawnTarget.Caster;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("相对于生成目标的偏移量")]
        public Vector3 Offset;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否跟随生成目标移动")]
        public bool FollowTarget = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("特效存活时间 (0 = 不自动销毁)")]
        public float Lifetime;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("特效缩放")]
        public float Scale = 1f;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => SpawnVFX(context));
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            SpawnVFX(context);
        }

        private void SpawnVFX(VisualContext context)
        {
            if (VFXPrefab == null) return;

            Transform parent = null;
            Vector3 spawnPosition = GetSpawnPosition(context, SpawnOn, Offset);

            GameObject targetObject = GetSpawnTarget(context, SpawnOn);
            if (targetObject != null && FollowTarget)
            {
                parent = targetObject.transform;
            }

            // 使用 Pool 或直接实例化
            GameObject vfxInstance = SpawnFromPool(VFXPrefab, spawnPosition, Quaternion.identity);

            if (vfxInstance != null)
            {
                // 设置父节点
                if (parent != null)
                {
                    vfxInstance.transform.SetParent(parent);
                    vfxInstance.transform.localPosition = Offset;
                }

                // 设置缩放
                if (Scale != 1f)
                {
                    vfxInstance.transform.localScale = Vector3.one * Scale;
                }

                // 自动销毁
                if (Lifetime > 0)
                {
                    DespawnAfterDelay(vfxInstance, Lifetime);
                }
            }
        }

        // ========================================
        // Pool 集成 (可选)
        // ========================================

        private static GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
#if ZEROENGINE_POOL
            // 使用 ZeroEngine.Pool
            if (ZeroEngine.Pool.PoolManager.Instance != null)
            {
                return ZeroEngine.Pool.PoolManager.Instance.Spawn(prefab, position, rotation);
            }
#endif
            // 回退到直接实例化
            return UnityEngine.Object.Instantiate(prefab, position, rotation);
        }

        private static void DespawnAfterDelay(GameObject obj, float delay)
        {
#if ZEROENGINE_POOL
            // 使用 ZeroEngine.Pool
            if (ZeroEngine.Pool.PoolManager.Instance != null)
            {
                // 延迟回收
                var mono = obj.GetComponent<MonoBehaviour>();
                if (mono != null)
                {
                    mono.StartCoroutine(DespawnCoroutine(obj, delay));
                    return;
                }
            }
#endif
            // 回退到直接销毁
            UnityEngine.Object.Destroy(obj, delay);
        }

#if ZEROENGINE_POOL
        private static System.Collections.IEnumerator DespawnCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                ZeroEngine.Pool.PoolManager.Instance?.Despawn(obj);
            }
        }
#endif
    }
}
