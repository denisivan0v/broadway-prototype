namespace NuClear.Broadway.Interfaces.Events
{
    public class ObjectDeletedEvent
    {
        public ObjectDeletedEvent(long id)
        {
            Id = id;
        }

        public long Id { get; }
    }
}