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
    public sealed class FlowKaleidoscopeConsumerGrain : FlowConsumerGrain, IFlowKaleidoscopeConsumerGrain
    {
        private const string ConsumerGroupToken = "roads-flow-kaleidoscope-consumer";

        private readonly ILogger<FlowKaleidoscopeConsumerGrain> _logger;

        public FlowKaleidoscopeConsumerGrain(
            ILogger<FlowKaleidoscopeConsumerGrain> logger,
            ReferenceObjectsClusterKafkaOptions kafkaOptions)
            : base(logger, kafkaOptions, ConsumerGroupToken, kafkaOptions.FlowKaleidoscopeTopic)
        {
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<string, string> message)
        {
            var xml = XElement.Parse(message.Value);
            var objectType = xml.Name.ToString();
            switch (objectType)
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
                    _logger.LogInformation($"{objectType}: Unknown object type.");

                    break;
            }
        }

        private async Task UpdateCategoryAsync(XElement xml)
        {
            var code = (long)xml.Attribute(nameof(Category.Code));
            var categoryGrain = GrainFactory.GetGrain<ICategoryGrain>(code);

            var isDeleted = (bool)xml.Attribute(nameof(Category.IsDeleted));
            if (!isDeleted)
            {
                var category = new Category { Code = code };

                var localizations = xml.Element(nameof(Category.Localizations));
                category.Localizations =
                    localizations?.Elements()
                                 .Select(
                                     x => new RubricLocalization
                                         {
                                             Lang = (string)x.Attribute(nameof(RubricLocalization.Lang)),
                                             Name = (string)x.Attribute(nameof(RubricLocalization.Name))
                                         })
                                 .ToList();

                await categoryGrain.UpdateStateAsync(category);
            }
            else
            {
                await categoryGrain.DeleteAsync(code);
            }
        }

        private async Task UpdateSecondRubricAsync(XElement xml)
        {
            var code = (long)xml.Attribute(nameof(SecondRubric.Code));
            var secondRubricGrain = GrainFactory.GetGrain<ISecondRubricGrain>(code);

            var isDeleted = (bool)xml.Attribute(nameof(SecondRubric.IsDeleted));
            if (!isDeleted)
            {
                var secondRubric = new SecondRubric
                    {
                        Code = code,
                        CategoryCode = (long)xml.Attribute(nameof(SecondRubric.CategoryCode))
                    };

                var localizations = xml.Element(nameof(SecondRubric.Localizations));
                secondRubric.Localizations =
                    localizations?.Elements()
                                 .Select(
                                     x => new RubricLocalization
                                         {
                                             Lang = (string)x.Attribute(nameof(RubricLocalization.Lang)),
                                             Name = (string)x.Attribute(nameof(RubricLocalization.Name))
                                         })
                                 .ToList();
                await secondRubricGrain.UpdateStateAsync(secondRubric);
            }
            else
            {
                await secondRubricGrain.DeleteAsync(code);
            }
        }

        private async Task UpdateRubricAsync(XElement xml)
        {
            var code = (long)xml.Attribute(nameof(Rubric.Code));
            var rubricGrain = GrainFactory.GetGrain<IRubricGrain>(code);

            var isDeleted = (bool)xml.Attribute(nameof(Rubric.IsDeleted));
            if (!isDeleted)
            {
                var rubric = new Rubric
                    {
                        Code = code,
                        SecondRubricCode = (long)xml.Attribute(nameof(Rubric.SecondRubricCode)),
                        IsCommercial = (bool)xml.Attribute(nameof(Rubric.IsCommercial))
                    };

                var localizations = xml.Element(nameof(Rubric.Localizations));
                rubric.Localizations =
                    localizations?.Elements()
                                 .Select(
                                     x => new RubricLocalization
                                         {
                                             Lang = (string)x.Attribute(nameof(RubricLocalization.Lang)),
                                             Name = (string)x.Attribute(nameof(RubricLocalization.Name)),
                                             ShortName = (string)x.Attribute(nameof(RubricLocalization.ShortName))
                                         })
                                 .ToList();

                rubric.Branches = xml.Elements("Groups")
                                     .Elements()
                                     .Elements(nameof(Rubric.Branches))
                                     .Elements()
                                     .Select(x => new RubricBranch { RubricCode = code, BranchCode = (int)x.Attribute(nameof(Rubric.Code)) })
                                     .ToHashSet();

                await rubricGrain.UpdateStateAsync(rubric);
            }
            else
            {
                await rubricGrain.DeleteAsync(code);
            }
        }
    }
}