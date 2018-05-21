using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

using NuClear.Broadway.Grains.Options;
using NuClear.Broadway.Interfaces;
using Orleans.Concurrency;
using NuClear.Broadway.Interfaces.Workers;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public class FlowCardForErmConsumerGrain : FlowConsumerGrain, IFlowCardForErmConsumerGrain
    {
        private const string ConsumerGroupToken = "roads-flow-cardsforerm-consumer";
        private const string Topic = "roads_test_flowCardsForERM";

        private readonly ILogger<FlowCardForErmConsumerGrain> _logger;

        public FlowCardForErmConsumerGrain(
            ILogger<FlowCardForErmConsumerGrain> logger,
            ReferenceObjectsClusterKafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, Topic)
        {
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var xml = XElement.Parse(message.Value);
            var objectType = xml.Name.ToString();
            switch (objectType)
            {
                case nameof(CardForERM):
                    await UpdateCardAsync(xml);

                    break;
                case "EmptyArchivedFirm":
                    await ArchiveFirm(xml);

                    break;
                default:
                    _logger.LogInformation($"{objectType}: Unknown object type.");

                    break;
            }
        }

        private async Task UpdateCardAsync(XElement xml)
        {
            var card = new CardForERM
                {
                    Code = (long)xml.Attribute(nameof(CardForERM.Code)),
                    BranchCode = (int)xml.Attribute(nameof(CardForERM.BranchCode)),
                    CountryCode = (int?)xml.Attribute(nameof(CardForERM.CountryCode)),
                    IsLinked = (bool)xml.Attribute(nameof(CardForERM.IsLinked)),
                    IsActive = (bool)xml.Attribute(nameof(CardForERM.IsActive)),
                    IsDeleted = (bool)xml.Attribute(nameof(CardForERM.IsDeleted)),
                    ClosedForAscertainment = (bool)xml.Attribute(nameof(CardForERM.ClosedForAscertainment)),
                    SortingPosition = (int)xml.Attribute(nameof(CardForERM.SortingPosition))
                };

            var address = xml.Element(nameof(CardForERM.Address));
            if (address != null)
            {
                card.Address = new CardForERM.FirmAddress
                    {
                        Text = (string)address.Attribute(nameof(CardForERM.FirmAddress.Text)),
                        TerritoryCode = (long?)address.Attribute(nameof(CardForERM.FirmAddress.TerritoryCode)),
                        BuildingPurposeCode = (int?)address.Attribute(nameof(CardForERM.FirmAddress.BuildingPurposeCode)),
                    };
            }

            var rubrics = xml.Element(nameof(CardForERM.Rubrics));
            if (rubrics != null)
            {
                card.Rubrics = rubrics.Elements()
                                      .Select(
                                          x => new CardForERM.Rubric
                                              {
                                                  Code = (long)x.Attribute(nameof(CardForERM.Rubric.Code)),
                                                  SortingPosition = (int)x.Attribute(nameof(CardForERM.Rubric.SortingPosition)),
                                                  IsPrimary = (bool)x.Attribute(nameof(CardForERM.Rubric.IsPrimary))
                                              })
                                      .ToHashSet();
            }

            var firmGrain = GrainFactory.GetGrain<IFirmGrain>(card.FirmCode);

            var cardGrain = GrainFactory.GetGrain<ICardForERMGrain>(card.Code);
            var firmCode = await cardGrain.GetFirmCodeAsync();
            if (firmCode != default && card.FirmCode != firmCode)
            {
                await firmGrain.RemoveCardAsync(card.Code);
            }
            else
            {
                await firmGrain.AddCardAsync(card.Code);
            }

            var firmElement = xml.Element(nameof(Firm));
            if (firmElement != null)
            {
                var firm = new Firm
                    {
                        Code = (long)firmElement.Attribute(nameof(Firm.Code)),
                        BranchCode = (int)firmElement.Attribute(nameof(Firm.BranchCode)),
                        Name = (string)firmElement.Attribute(nameof(Firm.Name)),
                        IsActive = (bool)firmElement.Attribute(nameof(Firm.IsActive)),
                        ClosedForAscertainment = (bool)firmElement.Attribute(nameof(Firm.ClosedForAscertainment))
                    };

                card.FirmCode = firm.Code;

                await firmGrain.UpdateStateAsync(firm);
            }

            await cardGrain.UpdateStateAsync(card);
        }

        private async Task ArchiveFirm(XElement xml)
        {
            var firmCode = (long)xml.Attribute(nameof(Firm.Code));
            var branchCode = (int)xml.Attribute(nameof(Firm.BranchCode));
            var countryCode = (int?)xml.Attribute(nameof(Firm.CountryCode));

            var firmGrain = GrainFactory.GetGrain<IFirmGrain>(firmCode);
            await firmGrain.Archive(branchCode, countryCode);
        }
    }
}