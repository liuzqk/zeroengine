// ============================================================================
// ISectMember.cs
// 门派成员接口
// 创建于: 2026-01-09
// ============================================================================

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派成员接口
    /// 任何可以加入门派的实体都应实现此接口
    /// </summary>
    public interface ISectMember
    {
        /// <summary>角色唯一 ID</summary>
        string CharacterId { get; }

        /// <summary>当前门派实例 (null 表示无门派)</summary>
        SectInstance CurrentSect { get; }

        /// <summary>是否有门派</summary>
        bool HasSect { get; }

        /// <summary>当前门派类型</summary>
        SectType SectType { get; }

        /// <summary>当前职位</summary>
        SectRank SectRank { get; }

        /// <summary>当前贡献度</summary>
        int Contribution { get; }

        /// <summary>
        /// 加入门派
        /// </summary>
        /// <param name="sectData">门派数据</param>
        /// <param name="initialRank">初始职位</param>
        /// <returns>是否成功</returns>
        bool JoinSect(SectDataSO sectData, SectRank initialRank = SectRank.Initiate);

        /// <summary>
        /// 离开门派
        /// </summary>
        /// <param name="reason">离开原因</param>
        /// <returns>是否成功</returns>
        bool LeaveSect(SectLeaveReason reason = SectLeaveReason.Voluntary);

        /// <summary>
        /// 增加贡献度
        /// </summary>
        void AddContribution(int amount);

        /// <summary>
        /// 修改声望
        /// </summary>
        void ModifyReputation(int delta);
    }
}
