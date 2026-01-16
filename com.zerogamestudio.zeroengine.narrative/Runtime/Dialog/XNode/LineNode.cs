#if XNODE_PRESENT
using UnityEngine;
using XNode;

namespace ZeroEngine.Dialog.XNodeIntegration
{
    [NodeWidth(300)]
    [CreateNodeMenu("Dialog/Line")]
    public class LineNode : BaseDialogNode
    {
        [Header("Content")]
        public string Speaker;
        [TextArea(3, 10)] public string Text;
        
        [Header("Metadata")]
        public string LocalizationKey;
        public string VoiceKey;
        public string PortraitKey;

        public override BaseDialogNode GetNext()
        {
            return base.GetNext();
        }
    }
}
#endif
