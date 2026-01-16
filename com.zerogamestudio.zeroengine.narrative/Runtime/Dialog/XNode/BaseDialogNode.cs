#if XNODE_PRESENT
using XNode;

namespace ZeroEngine.Dialog.XNodeIntegration
{
    public abstract class BaseDialogNode : Node
    {
        // Standard flow connections
        [Input(backingValue = ShowBackingValue.Never)] public Connection Input;
        [Output(backingValue = ShowBackingValue.Never)] public Connection Output;

        [System.Serializable] public class Connection { }

        /// <summary>
        /// Get the next node in the flow.
        /// </summary>
        public virtual BaseDialogNode GetNext()
        {
            NodePort port = GetOutputPort("Output");
            if (port != null && port.IsConnected)
            {
                return port.Connection.node as BaseDialogNode;
            }
            return null;
        }
    }
}
#endif
