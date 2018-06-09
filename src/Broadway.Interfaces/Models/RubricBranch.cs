using Newtonsoft.Json;

namespace NuClear.Broadway.Interfaces.Models
{
    public class RubricBranch
    {
        public long RubricCode { get; set; }
        [JsonIgnore]
        public Rubric Rubric { get; set; }
        public int BranchCode { get; set; }
    }
}