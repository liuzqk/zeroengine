using System;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>生成抛射物效果</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("生成抛射物")]
    [Sirenix.OdinInspector.GUIColor(0.8f, 0.6f, 1f)]
#endif
    public class SpawnProjectileEffectData : EffectComponentData
    {
        public GameObject ProjectilePrefab;
        public float Speed = 10f;
        public int Count = 1;
    }
}
