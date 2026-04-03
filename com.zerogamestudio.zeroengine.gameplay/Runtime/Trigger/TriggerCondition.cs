using System;
using UnityEngine;

namespace ZeroEngine.Trigger
{
    /// <summary>
    /// Abstract base for conditions that gate trigger execution.
    /// </summary>
    [Serializable]
    public abstract class TriggerCondition
    {
        /// <summary>
        /// Return true if the trigger is allowed to fire.
        /// </summary>
        public abstract bool Evaluate(GameObject owner, GameObject activator);
    }
}
