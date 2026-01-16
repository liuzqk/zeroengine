namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 叶节点抽象基类，不包含子节点
    /// </summary>
    public abstract class BTLeaf : BTNode
    {
        // 叶节点不需要额外的逻辑，直接继承 BTNode
    }
}
