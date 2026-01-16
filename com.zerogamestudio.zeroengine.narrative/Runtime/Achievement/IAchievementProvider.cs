namespace ZeroEngine.Achievement
{
    /// <summary>
    /// Interface for external achievement providers (e.g. Steam, Epic, Custom Server).
    /// </summary>
    public interface IAchievementProvider
    {
        /// <summary>
        /// Initialize the provider.
        /// </summary>
        /// <returns>True if initialization successful.</returns>
        bool Initialize();

        /// <summary>
        /// Unlock an achievement in the provider.
        /// </summary>
        void Unlock(string id);

        /// <summary>
        /// Set progress for an achievement.
        /// </summary>
        void SetProgress(string id, float progress);
    }
}
