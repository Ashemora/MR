namespace Project.Scripts.Services.Clock
{
    public interface IBattleClock
    {
        long CurrentTick { get; }
        int TickRate { get; }
        int SecondsToTicks(float seconds);
    }
}