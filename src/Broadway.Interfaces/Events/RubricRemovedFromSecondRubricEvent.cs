namespace NuClear.Broadway.Interfaces.Events
{
    public class RubricRemovedFromSecondRubricEvent
    {
        public RubricRemovedFromSecondRubricEvent(long rubricCode)
        {
            RubricCode = rubricCode;
        }

        public long RubricCode { get; }
    }
}