#if XNODE_PRESENT
using UnityEngine;

namespace ZeroEngine.Dialog.XNodeIntegration
{
    [NodeTint("#00FF00")]
    [CreateNodeMenu("Dialog/Start")]
    public class StartNode : BaseDialogNode
    {
        // StartNode only has Output, no Input port needed
        // Input is inherited from BaseDialogNode but can be ignored
    }
}
#endif
