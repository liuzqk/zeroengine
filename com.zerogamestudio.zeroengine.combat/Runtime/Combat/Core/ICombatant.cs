using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 战斗单位接口 - 所有可参与战斗的对象必须实现此接口
    /// </summary>
    public interface ICombatant
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        string CombatantId { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 所属阵营/队伍ID
        /// </summary>
        int TeamId { get; }

        /// <summary>
        /// 是否存活
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 是否可被选中为目标
        /// </summary>
        bool IsTargetable { get; }

        /// <summary>
        /// 获取 GameObject 引用
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// 获取 Transform 引用
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// 获取战斗位置（用于距离计算）
        /// </summary>
        Vector3 GetCombatPosition();

        /// <summary>
        /// 接收伤害
        /// </summary>
        /// <param name="damage">伤害数据</param>
        /// <returns>实际造成的伤害结果</returns>
        DamageResult TakeDamage(DamageData damage);

        /// <summary>
        /// 接收治疗
        /// </summary>
        /// <param name="amount">治疗量</param>
        /// <param name="source">治疗来源</param>
        /// <returns>实际治疗量</returns>
        float ReceiveHeal(float amount, ICombatant source = null);

        /// <summary>
        /// 当进入战斗时调用
        /// </summary>
        void OnEnterCombat();

        /// <summary>
        /// 当离开战斗时调用
        /// </summary>
        void OnExitCombat();
    }

    /// <summary>
    /// 战斗单位状态
    /// </summary>
    public enum CombatantState
    {
        /// <summary>空闲状态</summary>
        Idle,
        /// <summary>战斗中</summary>
        InCombat,
        /// <summary>死亡</summary>
        Dead,
        /// <summary>无法行动（眩晕等）</summary>
        Incapacitated
    }
}
