using System;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋事件参数
    /// </summary>
    public struct TalentEventArgs
    {
        public TalentEventType EventType;
        public TalentNodeSO Node;
        public int OldLevel;
        public int NewLevel;
        public int PointsSpent;

        public TalentEventArgs(TalentEventType eventType, TalentNodeSO node = null,
            int oldLevel = 0, int newLevel = 0, int pointsSpent = 0)
        {
            EventType = eventType;
            Node = node;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            PointsSpent = pointsSpent;
        }

        public static TalentEventArgs Unlocked(TalentNodeSO node, int level, int points)
            => new TalentEventArgs(TalentEventType.NodeUnlocked, node, 0, level, points);

        public static TalentEventArgs LevelUp(TalentNodeSO node, int oldLevel, int newLevel, int points)
            => new TalentEventArgs(TalentEventType.NodeLevelUp, node, oldLevel, newLevel, points);

        public static TalentEventArgs Reset(TalentNodeSO node, int oldLevel, int refundedPoints)
            => new TalentEventArgs(TalentEventType.NodeReset, node, oldLevel, 0, refundedPoints);

        public static TalentEventArgs TreeReset(int refundedPoints)
            => new TalentEventArgs(TalentEventType.TreeReset, null, 0, 0, refundedPoints);

        public static TalentEventArgs PointsChanged(int newPoints)
            => new TalentEventArgs(TalentEventType.PointsChanged, null, 0, 0, newPoints);
    }
}
