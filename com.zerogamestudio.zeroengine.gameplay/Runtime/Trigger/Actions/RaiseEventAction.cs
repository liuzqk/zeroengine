using System;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Trigger.Actions
{
    /// <summary>
    /// Raises a global event via EventManager.
    /// </summary>
    [Serializable]
    public class RaiseEventAction : TriggerAction
    {
        [Tooltip("Event name to raise")]
        public string EventName;

        public override void Execute(GameObject owner, GameObject activator)
        {
            if (!string.IsNullOrEmpty(EventName))
                EventManager.Trigger(EventName);
        }
    }
}
