using System;
using UnityEngine;

namespace ZeroEngine.Trigger.Actions
{
    /// <summary>
    /// Debug action that logs a message when triggered.
    /// </summary>
    [Serializable]
    public class LogAction : TriggerAction
    {
        [Tooltip("Message to log")]
        public string Message = "Trigger fired";

        public override void Execute(GameObject owner, GameObject activator)
        {
            Debug.Log($"[EventTriggerZone] {Message} | Owner={owner?.name}, Activator={activator?.name}");
        }
    }
}
