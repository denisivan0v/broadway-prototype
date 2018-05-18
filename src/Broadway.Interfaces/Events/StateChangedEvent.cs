namespace NuClear.Broadway.Interfaces.Events
{
    public class StateChangedEvent<TState>
    {
        public StateChangedEvent(TState state)
        {
            State = state;
        }

        public TState State { get; }
    }
}