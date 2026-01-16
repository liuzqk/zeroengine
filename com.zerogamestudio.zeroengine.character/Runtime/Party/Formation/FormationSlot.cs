using System;
using UnityEngine;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 阵型槽位 - 定义阵型中一个位置的属性
    /// </summary>
    [Serializable]
    public class FormationSlot
    {
        [Tooltip("槽位索引 (对应 PartySlot.Index)")]
        public int SlotIndex;

        [Tooltip("相对位置偏移 (本地坐标)")]
        public Vector3 LocalPosition;

        [Tooltip("相对旋转偏移 (本地欧拉角)")]
        public Vector3 LocalRotation;

        [Tooltip("位置类型")]
        public FormationPosition PositionType = FormationPosition.Middle;

        [Tooltip("角色朝向")]
        public FormationFacing Facing = FormationFacing.Forward;

        [Tooltip("防御修正 (前排低/后排高)")]
        [Range(-0.5f, 0.5f)]
        public float DefenseModifier = 0f;

        [Tooltip("攻击修正 (前排高/后排低)")]
        [Range(-0.5f, 0.5f)]
        public float AttackModifier = 0f;

        [Tooltip("仇恨权重 (前排高/后排低)")]
        [Range(0f, 2f)]
        public float ThreatWeight = 1f;

        /// <summary>
        /// 获取世界坐标位置
        /// </summary>
        public Vector3 GetWorldPosition(Transform formationAnchor)
        {
            if (formationAnchor == null)
                return LocalPosition;

            return formationAnchor.TransformPoint(LocalPosition);
        }

        /// <summary>
        /// 获取世界旋转
        /// </summary>
        public Quaternion GetWorldRotation(Transform formationAnchor)
        {
            var localRot = Quaternion.Euler(LocalRotation);
            if (formationAnchor == null)
                return localRot;

            return formationAnchor.rotation * localRot;
        }

        public FormationSlot Clone()
        {
            return new FormationSlot
            {
                SlotIndex = SlotIndex,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation,
                PositionType = PositionType,
                Facing = Facing,
                DefenseModifier = DefenseModifier,
                AttackModifier = AttackModifier,
                ThreatWeight = ThreatWeight
            };
        }
    }

    /// <summary>
    /// 阵型位置类型 (用于战术 AI)
    /// </summary>
    public enum FormationPosition
    {
        /// <summary>最前排 (坦克)</summary>
        Front,

        /// <summary>中排</summary>
        Middle,

        /// <summary>后排 (远程/法师)</summary>
        Back,

        /// <summary>侧翼</summary>
        Flank,

        /// <summary>中心 (指挥官)</summary>
        Center
    }

    /// <summary>
    /// 角色朝向
    /// </summary>
    public enum FormationFacing
    {
        /// <summary>朝前</summary>
        Forward,

        /// <summary>朝左</summary>
        Left,

        /// <summary>朝右</summary>
        Right,

        /// <summary>朝后</summary>
        Backward,

        /// <summary>朝向中心</summary>
        TowardCenter,

        /// <summary>背向中心</summary>
        AwayFromCenter
    }

    /// <summary>
    /// 阵型事件参数
    /// </summary>
    public readonly struct FormationChangedEventArgs
    {
        public readonly FormationSO OldFormation;
        public readonly FormationSO NewFormation;

        public FormationChangedEventArgs(FormationSO oldFormation, FormationSO newFormation)
        {
            OldFormation = oldFormation;
            NewFormation = newFormation;
        }
    }

    /// <summary>
    /// 成员位置更新事件
    /// </summary>
    public readonly struct MemberPositionUpdatedEventArgs
    {
        public readonly IPartyMember Member;
        public readonly Vector3 OldPosition;
        public readonly Vector3 NewPosition;
        public readonly FormationSlot Slot;

        public MemberPositionUpdatedEventArgs(
            IPartyMember member,
            Vector3 oldPos,
            Vector3 newPos,
            FormationSlot slot)
        {
            Member = member;
            OldPosition = oldPos;
            NewPosition = newPos;
            Slot = slot;
        }
    }
}
