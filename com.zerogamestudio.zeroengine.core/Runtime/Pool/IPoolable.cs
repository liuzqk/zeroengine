namespace ZeroEngine.Pool
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的组件在进出对象池时会自动调用对应方法
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 当从池中取出时调用（替代 Start/Awake 进行初始化）
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 当归还到池中时调用（用于重置状态，停止特效等）
        /// </summary>
        void OnDespawn();
    }
}
