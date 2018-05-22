namespace NuClear.Broadway.Interfaces.Events
{
    public class SecondRubricRemovedFromCategoryEvent
    {
        public SecondRubricRemovedFromCategoryEvent(long secondRubricCode)
        {
            SecondRubricCode = secondRubricCode;
        }

        public long SecondRubricCode { get; }
    }
}