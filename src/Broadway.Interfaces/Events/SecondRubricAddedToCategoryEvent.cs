namespace NuClear.Broadway.Interfaces.Events
{
    public class SecondRubricAddedToCategoryEvent
    {
        public SecondRubricAddedToCategoryEvent(long secondRubricCode)
        {
            SecondRubricCode = secondRubricCode;
        }

        public long SecondRubricCode { get; }
    }
}