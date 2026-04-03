using System;
using UnityEngine;
using ZeroEngine.Trigger;

namespace ZeroEngine.Quest.Trigger
{
    /// <summary>
    /// Trigger condition that checks the state of a quest.
    /// </summary>
    [Serializable]
    public class QuestStateCondition : TriggerCondition
    {
        [Tooltip("Quest ID to check")]
        public string QuestId;

        [Tooltip("Required quest state")]
        public QuestState RequiredState = QuestState.Inactive;

        public override bool Evaluate(GameObject owner, GameObject activator)
        {
            if (string.IsNullOrEmpty(QuestId)) return false;

            if (QuestManager.Instance == null) return false;
            return QuestManager.Instance.GetQuestState(QuestId) == RequiredState;
        }
    }
}
