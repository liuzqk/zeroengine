#if XNODE_PRESENT
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace ZeroEngine.Dialog.XNodeIntegration
{
    [CreateNodeMenu("Dialog/Choice")]
    public class ChoiceNode : BaseDialogNode
    {
        [System.Serializable]
        public class ChoiceOption
        {
            public string Text;
            public string LocalizationKey;
            public string Condition;
        }

        [Output(dynamicPortList = true)] public List<ChoiceOption> Choices = new List<ChoiceOption>();

        // ChoiceNode doesn't have a single "Next", the flow depends on selection.
        public override BaseDialogNode GetNext()
        {
            return null;
        }

        public BaseDialogNode GetChoiceNode(int index)
        {
            // dynamicPortList name format is "FieldName index" usually, e.g. "Choices 0"
            // Use xNode API to find the connection
            string portName = "Choices " + index;
            NodePort port = GetOutputPort(portName);
            if (port != null && port.IsConnected)
            {
                return port.Connection.node as BaseDialogNode;
            }
            return null;
        }
    }
}
#endif
