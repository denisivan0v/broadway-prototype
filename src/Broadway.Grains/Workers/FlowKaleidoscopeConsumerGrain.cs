using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Grains.Options;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Workers;
using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker]
    public sealed class FlowKaleidoscopeConsumerGrain : FlowConsumerGrain, IFlowKaleidoscopeConsumerGrain
    {
        private const string ConsumerGroupToken = "roads-flow-kaleidoscope-consumer";
        private const string Topic = "casino_staging_flowKaleidoscope_compacted";
        
        private readonly ILogger<FlowKaleidoscopeConsumerGrain> _logger;
        
        public FlowKaleidoscopeConsumerGrain(ILogger<FlowKaleidoscopeConsumerGrain> logger, ReferenceObjectsClusterKafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, Topic)
        {
            _logger = logger;
        }
        
        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var xml = XElement.Parse(message.Value);
            switch (xml.Name.ToString())
            {
                case nameof(Category):
                    await UpdateCategoryAsync(xml);
                    break;
                case nameof(SecondRubric):
                    await UpdateSecondRubricAsync(xml);
                    break;
                case nameof(Rubric):
                    await UpdateRubricAsync(xml);
                    break;
                default:
                    _logger.LogInformation("Unknown object type.");
                    break;
            }
        }

        private async Task UpdateCategoryAsync(XElement xml)
        {
            var category = new Category
            {
                Code = (long) xml.Attribute(nameof(Category.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(Category.IsDeleted)),
            };

            if (!category.IsDeleted)
            {
                var localizations = xml.Element(nameof(Category.Localizations));
                category.Localizations = localizations?.Elements()
                    .Select(x => new Localization(
                        (string) x.Attribute(nameof(Localization.Lang)),
                        (string) x.Attribute(nameof(Localization.Name))))
                    .ToHashSet();
            }

            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(category.Code);
            await categoryGrain.UpdateStateAsync(category);
        }
        
        private async Task UpdateSecondRubricAsync(XElement xml)
        {
            var secondRubric = new SecondRubric
            {
                Code = (long) xml.Attribute(nameof(SecondRubric.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(SecondRubric.IsDeleted)),
            };

            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(secondRubric.CategoryCode);
            if (!secondRubric.IsDeleted)
            {
                secondRubric.CategoryCode = (long) xml.Attribute(nameof(SecondRubric.CategoryCode));
                await categoryGrain.AddSecondRubric(secondRubric.Code);
                
                var localizations = xml.Element(nameof(SecondRubric.Localizations));
                secondRubric.Localizations = localizations?.Elements()
                    .Select(x => new Localization(
                        (string) x.Attribute(nameof(Localization.Lang)),
                        (string) x.Attribute(nameof(Localization.Name))))
                    .ToHashSet();
            }
            else
            {
                await categoryGrain.RemoveSecondRubric(secondRubric.Code);
            }
            
            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(secondRubric.Code);
            await secondRubricGrain.UpdateStateAsync(secondRubric);
        }
        
        private async Task UpdateRubricAsync(XElement xml)
        {
            var rubric = new Rubric
            {
                Code = (long) xml.Attribute(nameof(Rubric.Code)),
                IsDeleted = (bool) xml.Attribute(nameof(Rubric.IsDeleted)),
            };

            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(rubric.SecondRubricCode);
            if (!rubric.IsDeleted)
            {
                rubric.SecondRubricCode = (long) xml.Attribute(nameof(Rubric.SecondRubricCode));
                await secondRubricGrain.AddRubric(rubric.Code);
                
                rubric.IsCommercial = (bool) xml.Attribute(nameof(Rubric.IsCommercial));
                
                var localizations = xml.Element(nameof(Rubric.Localizations));
                rubric.Localizations = localizations?.Elements()
                    .Select(x => new Localization(
                        (string) x.Attribute(nameof(Localization.Lang)),
                        (string) x.Attribute(nameof(Localization.Name)),
                        (string) x.Attribute(nameof(Localization.ShortName))))
                    .ToHashSet();
                rubric.Branches = xml.Elements("Groups")
                    .Elements()
                    .Elements(nameof(Rubric.Branches))
                    .Elements()
                    .Select(x => (int) x.Attribute(nameof(Rubric.Code)))
                    .ToHashSet();
            }
            else
            {
                await secondRubricGrain.RemoveRubric(rubric.Code);
            }

            var rubricGrain = GrainFactory.GetGrain<IRubricGrain>(rubric.Code);
            await rubricGrain.UpdateStateAsync(rubric);
        }
    }
}