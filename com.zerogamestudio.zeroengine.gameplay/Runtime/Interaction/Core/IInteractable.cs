using UnityEngine;

namespace ZeroEngine.Interaction
{
    /// <summary>
    /// 可交互对象接口 (v1.14.0+)
    /// 所有可交互对象必须实现此接口
    /// </summary>
    public interface IInteractable
    {
        /// <summary>唯一标识符 (用于任务系统等)</summary>
        string InteractableId { get; }

        /// <summary>显示名称 (用于 UI 提示)</summary>
        string DisplayName { get; }

        /// <summary>交互类型</summary>
        InteractionType Type { get; }

        /// <summary>交互优先级 (同距离时优先选择高优先级)</summary>
        InteractionPriority Priority { get; }

        /// <summary>是否启用交互</summary>
        bool IsEnabled { get; }

        /// <summary>获取交互提示文本</summary>
        string GetInteractionHint();

        /// <summary>
        /// 检查是否可以交互
        /// </summary>
        /// <param name="ctx">交互上下文</param>
        /// <returns>是否可交互</returns>
        bool CanInteract(InteractionContext ctx);

        /// <summary>
        /// 获取不可交互的原因 (用于 UI 提示)
        /// </summary>
        /// <param name="ctx">交互上下文</param>
        /// <returns>不可交互原因，可交互时返回 null</returns>
        string GetCannotInteractReason(InteractionContext ctx);

        /// <summary>
        /// 执行交互
        /// </summary>
        /// <param name="ctx">交互上下文</param>
        /// <returns>交互结果</returns>
        InteractionResult OnInteract(InteractionContext ctx);

        /// <summary>
        /// 当此对象获得交互焦点时调用
        /// (玩家接近且成为最近可交互对象)
        /// </summary>
        void OnFocus();

        /// <summary>
        /// 当此对象失去交互焦点时调用
        /// </summary>
        void OnUnfocus();

        /// <summary>获取交互位置 (World Space)</summary>
        Vector3 GetInteractionPosition();

        /// <summary>获取 GameObject 引用</summary>
        GameObject GameObject { get; }
    }
}
