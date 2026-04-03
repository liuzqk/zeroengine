namespace ZeroEngine.Trigger
{
    public enum TriggerRepeatMode
    {
        Once,        // Trigger once ever
        OncePerRun,  // Reset each run
        EveryEntry,  // Trigger every time
        Count        // Trigger up to N times
    }
}
