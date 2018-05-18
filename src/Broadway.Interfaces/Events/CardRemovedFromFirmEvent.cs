namespace NuClear.Broadway.Interfaces.Events
{
    public class CardRemovedFromFirmEvent
    {
        public CardRemovedFromFirmEvent(long cardCode)
        {
            CardCode = cardCode;
        }

        public long CardCode { get; }
    }
}