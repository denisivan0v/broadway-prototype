namespace NuClear.Broadway.Interfaces.Events
{
    public class FirmArchivedEvent
    {
        public long FirmCode { get; }
        public int BranchCode { get; }
        public int? CountryCode { get; }

        public FirmArchivedEvent(long firmCode, int branchCode, int? countryCode)
        {
            FirmCode = firmCode;
            BranchCode = branchCode;
            CountryCode = countryCode;
        }
    }
}