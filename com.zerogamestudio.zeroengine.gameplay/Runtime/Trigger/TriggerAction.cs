using System;
using UnityEngine;

namespace ZeroEngine.Trigger
{
    /// <summary>
    /// Abstract base for actions executed when a trigger fires.
    /// </summary>
    [Serializable]
    public abstract class TriggerAction
    {
        [Tooltip("Delay in seconds before executing this action")]
        public float Delay;

        /// <summary>
        /// Execute this action.
        /// </summary>
        public abstract void Execute(GameObject owner, GameObject activator);
    }
}
