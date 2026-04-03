using System;
using UnityEngine;
using ZeroEngine.Trigger;

namespace ZeroEngine.Quest.Trigger
{
    /// <summary>
    /// Trigger condition that checks whether a quest is currently active.
    /// </summary>
    [Serializable]
    public class HasQuestCondition : TriggerCondition
    {
        [Tooltip("Quest ID to check")]
        public string QuestId;

        [Tooltip("Invert — pass when quest is NOT active")]
        public bool Invert;

        public override bool Evaluate(GameObject owner, GameObject activator)
        {
            if (string.IsNullOrEmpty(QuestId)) return false;

            bool active = QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest(QuestId);
            return Invert ? !active : active;
        }
    }
}
