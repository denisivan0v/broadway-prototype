using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NuClear.Broadway.Grains.Options;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;
using NuClear.Broadway.Interfaces.Workers;

using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public class FlowGeoClassifierConsumerGrain : FlowConsumerGrain, IFlowGeoClassifierConsumerGrain
    {
        private const string ConsumerGroupToken = "roads-flow-geoclassifier-consumer";

        private readonly ILogger<FlowGeoClassifierConsumerGrain> _logger;

        public FlowGeoClassifierConsumerGrain(
            ILogger<FlowGeoClassifierConsumerGrain> logger,
            ReferenceObjectsClusterKafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, kafkaOptions.FlowGeoClassifierTopic)
        {
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var xml = XElement.Parse(message.Value);
            var objectType = xml.Name.ToString();
            switch (objectType)
            {
                case nameof(Branch):
                    await UpdateBranchAsync(xml);
                    break;
                default:
                    _logger.LogInformation($"{objectType}: Unknown object type.");
                    break;
            }
        }

        private async Task UpdateBranchAsync(XElement xml)
        {
            var code = (int)xml.Attribute(nameof(Branch.Code));
            var branchGrain = GrainFactory.GetGrain<IBranchGrain>(code);

            var isDeleted = (bool?)xml.Attribute(nameof(Branch.IsDeleted)) ?? false;
            if (!isDeleted)
            {
                var branch = new Branch
                    {
                        Code = code,
                        NameLat = xml.Attribute(nameof(Branch.NameLat))?.Value,
                        DefaultCountryCode = (int)xml.Attribute(nameof(Branch.DefaultCountryCode)),
                        DefaultCityCode = (long?)xml.Attribute(nameof(Branch.DefaultCityCode)),
                        DefaultLang = xml.Attribute(nameof(Branch.DefaultLang))?.Value,
                        IsOnInfoRussia = (bool?)xml.Attribute(nameof(Branch.IsOnInfoRussia)) ?? true,
                        IsDeleted = false
                    };

                var localizations = xml.Element(nameof(Branch.Localizations));
                branch.Localizations = localizations?.Elements()
                                                    .Select(
                                                        x => new BranchLocalization
                                                            {
                                                                Lang = x.Attribute(nameof(BranchLocalization.Lang))?.Value,
                                                                Name = x.Attribute(nameof(BranchLocalization.Name))?.Value
                                                            })
                                                    .ToList();

                var enabledLanguages = xml.Element(nameof(Branch.EnabledLanguages));
                branch.EnabledLanguages = enabledLanguages?.Elements()
                                                    .Select(x => x.Attribute(nameof(BranchLocalization.Lang))?.Value)
                                                    .ToList();

                await branchGrain.UpdateStateAsync(branch);
            }
            else
            {
                await branchGrain.DeleteAsync(code);
            }
        }
    }
}
