namespace NuClear.Broadway.Interfaces.Events
{
    public class CardAddedToFirmEvent
    {
        public CardAddedToFirmEvent(long cardCode)
        {
            CardCode = cardCode;
        }

        public long CardCode { get; }
    }
}