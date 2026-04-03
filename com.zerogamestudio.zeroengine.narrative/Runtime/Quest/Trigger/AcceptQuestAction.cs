using System;
using UnityEngine;
using ZeroEngine.Trigger;

namespace ZeroEngine.Quest.Trigger
{
    /// <summary>
    /// Trigger action that accepts a quest via QuestManager.
    /// </summary>
    [Serializable]
    public class AcceptQuestAction : TriggerAction
    {
        [Tooltip("Quest ID to accept")]
        public string QuestId;

        public override void Execute(GameObject owner, GameObject activator)
        {
            if (string.IsNullOrEmpty(QuestId)) return;
            QuestManager.Instance?.AcceptQuest(QuestId);
        }
    }
}
