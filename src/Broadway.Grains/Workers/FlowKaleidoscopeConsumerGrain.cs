using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces;
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;
using Orleans;
using Orleans.Concurrency;

namespace NuClear.Broadway.Grains.Workers
{
    [StatelessWorker(1)]
    public class FlowKaleidoscopeConsumerGrain : Grain, IFlowKaleidoscopeConsumerGrain
    {
        private const int BufferSize = 100;
        private const string ConsumerGroupId = "roads-flow-kaleidoscope-consumer";
        
        private readonly ILogger<FlowKaleidoscopeConsumerGrain> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly BufferBlock<Message<string, string>> _messageProcessingQueue = new BufferBlock<Message<string, string>>();
        
        private SimpleMessageReceiver _messageReceiver;
        
        public FlowKaleidoscopeConsumerGrain(
            ILogger<FlowKaleidoscopeConsumerGrain> logger,
            KafkaOptions kafkaOptions)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions;
        }
        
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            
            _messageReceiver = new SimpleMessageReceiver(
                _logger,
                _kafkaOptions,
                $"{ConsumerGroupId}-{_kafkaOptions.ConsumerGroupToken}",
                new[] {"casino_staging_flowKaleidoscope_compacted"});

            _messageReceiver.OnMessage += OnMessage;
        }

        public override Task OnDeactivateAsync()
        {
            _messageReceiver.OnMessage -= OnMessage;
            _messageReceiver.Dispose();
            
            return base.OnDeactivateAsync();
        }
        
        public async Task StartExecutingAsync(GrainCancellationToken cancellation)
        {
            var waitHandler = new ManualResetEventSlim(false);

            var consumingTask = Task.Run(() =>
            {
                while (!cancellation.CancellationToken.IsCancellationRequested)
                {
                    if (_messageProcessingQueue.Count >= BufferSize)
                    {
                        _logger.LogDebug("Enabling consumer backpressure...");
                        waitHandler.Reset();
                        waitHandler.Wait();
                    }

                    _messageReceiver.Poll();
                }
            });

            while (await _messageProcessingQueue.OutputAvailableAsync(cancellation.CancellationToken))
            {
                var message = await _messageProcessingQueue.ReceiveAsync();
                if (_messageProcessingQueue.Count < BufferSize && !waitHandler.IsSet)
                {
                    waitHandler.Set();
                    _logger.LogDebug("Consumer backpressure disabled.");
                }

                await ProcessMessage(message);
            }
        }

        private void OnMessage(object sender, Message<string, string> message) => _messageProcessingQueue.Post(message);

        private async Task ProcessMessage(Message<string, string> message)
        {
            var xml = XElement.Parse(message.Value);
            switch (xml.Name.ToString())
            {
                case "Category":
                    await UpdateCategoryAsync(xml);
                    break;
                case "SecondRubric":
                    await UpdateSecondRubricAsync(xml);
                    break;
                case "Rubric":
                    await UpdateRubricAsync(xml);
                    break;
                default:
                    _logger.LogInformation("Unknown object type.");
                    break;
            }

            //await _messageReceiver.CommitAsync(message);
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

            if (!secondRubric.IsDeleted)
            {
                secondRubric.CategoryCode = (long) xml.Attribute(nameof(SecondRubric.CategoryCode));
                
                var localizations = xml.Element(nameof(SecondRubric.Localizations));
                secondRubric.Localizations = localizations?.Elements()
                    .Select(x => new Localization(
                        (string) x.Attribute(nameof(Localization.Lang)),
                        (string) x.Attribute(nameof(Localization.Name))))
                    .ToHashSet();
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

            if (!rubric.IsDeleted)
            {
                rubric.SecondRubricCode = (long) xml.Attribute(nameof(Rubric.SecondRubricCode));
                rubric.IsCommercial = (bool) xml.Attribute(nameof(Rubric.IsCommercial));
                
                var localizations = xml.Element(nameof(Rubric.Localizations));
                rubric.Localizations = localizations?.Elements()
                    .Select(x => new Localization(
                        (string) x.Attribute(nameof(Localization.Lang)),
                        (string) x.Attribute(nameof(Localization.Name)),
                        (string) x.Attribute(nameof(Localization.ShortName))))
                    .ToHashSet();
            }

            var secondRubricGrain = GrainFactory.GetGrain<IRubricGrain>(rubric.Code);
            await secondRubricGrain.UpdateStateAsync(rubric);
        }
    }
}