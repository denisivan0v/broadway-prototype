namespace NuClear.Broadway.Interfaces.Events
{
    public class RubricAddedToSecondRubricEvent
    {
        public RubricAddedToSecondRubricEvent(long rubricCode)
        {
            RubricCode = rubricCode;
        }

        public long RubricCode { get; }
    }
}