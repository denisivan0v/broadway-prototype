using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NuClear.Broadway.DataProjection;
using NuClear.Broadway.Grains;
using NuClear.Broadway.Grains.Options;
using NuClear.Broadway.Interfaces.Grains;
using NuClear.Broadway.Interfaces.Models;
using NuClear.Broadway.Kafka;
using NuClear.Broadway.Silo.Concurrency;
using NuClear.Broadway.Silo.StartupTasks;

using Orleans;
using Orleans.Clustering.Cassandra;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence.Cassandra;

using Serilog;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NuClear.Broadway.Silo
{
    internal class Program
    {
        private static readonly object SyncLock = new object();
        private static readonly ManualResetEvent SiloStopped = new ManualResetEvent(false);

        private static bool _siloStopping;

        private static void Main(string[] args)
        {
            var siloHost = CreateSilo();

            var logger = siloHost.Services.GetRequiredService<ILogger<Program>>();

            SetupApplicationShutdown(siloHost, logger);

            StartSilo(siloHost, logger).GetAwaiter().GetResult();

            SiloStopped.WaitOne();
        }

        private static void SetupApplicationShutdown(ISiloHost siloHost, ILogger logger)
        {
            Console.CancelKeyPress += (s, a) =>
            {
                a.Cancel = true;
                lock (SyncLock)
                {
                    if (!_siloStopping)
                    {
                        _siloStopping = true;
                        Task.Run(() => StopSilo(siloHost, logger)).Ignore();
                    }
                }
            };
        }

        private static ISiloHost CreateSilo()
        {
            var env = Environment.GetEnvironmentVariable("ROADS_ENVIRONMENT") ?? "Production";
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Serilog.ILogger logger = null;

            return new SiloHostBuilder()
                   .UseEnvironment(env)
                   .ConfigureAppConfiguration(
                       builder => builder.SetBasePath(basePath)
                                         .AddJsonFile("appsettings.json")
                                         .AddJsonFile($"appsettings.{env.ToLower()}.json")
                                         .AddEnvironmentVariables("ROADS_"))
                   .ConfigureServices(
                       (context, services) =>
                           {
                               var configuration = context.Configuration;

                               logger = CreateSerilogLogger(configuration);

                               var kafkaOptions = configuration.GetSection("Kafka").Get<KafkaOptions>();

                               var referenceObjectsClusterKafkaOptions =
                                   configuration.GetSection("ReferenceObjectsKafkaCluster").Get<ReferenceObjectsClusterKafkaOptions>();
                               services.AddSingleton(kafkaOptions.MergeWith(referenceObjectsClusterKafkaOptions));

                               var mainClusterKafkaOptions = configuration.GetSection("MainKafkaCluster").Get<KafkaOptions>();
                               services.AddSingleton(kafkaOptions.MergeWith(mainClusterKafkaOptions));

                               var connectionString = configuration.GetConnectionString("BroadwayDataProjection");
                               services.AddDbContextPool<DataProjectionContext>(builder => builder.UseNpgsql(connectionString));

                               services.AddTransient<MessageSender>()
                                       .AddTransient<IDataProjector<Category>, CategoryDataProjector>()
                                       .AddTransient<IDataProjector<SecondRubric>, SecondRubricDataProjector>()
                                       .AddTransient<IDataProjector<Rubric>, RubricDataProjector>()
                                       .AddTransient<IDataProjector<Firm>, FirmDataProjector>()
                                       .AddTransient<IDataProjector<CardForERM>, CardForERMDataProjector>();
                           })
                   .Configure<ClusterOptions>(
                       options =>
                           {
                               options.ClusterId = "broadway-prototype";
                               options.ServiceId = "broadway";
                           })
                   .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                   .UseCassandraClustering(config => config.GetSection("Cassandra"))
                   .AddCassandraGrainStorageAsDefault(config => config.GetSection("Cassandra"), ConcurrentGrainStateTypesProvider.Instance)
                   .AddLogStorageBasedLogConsistencyProvider("LogStorage")
                   .AddStateStorageBasedLogConsistencyProviderAsDefault()
                   .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CampaignGrain).Assembly).WithReferences())
                   .ConfigureLogging(logging => logging.AddSerilog(logger, true))
                   .AddIncomingGrainCallFilter<StateModificationCallFilter>()
                   .AddStartupTask<RunFlowConsumersStartupTask>()
                   .Build();
        }

        private static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration);
            return loggerConfiguration.CreateLogger();
        }

        private static async Task StartSilo(ISiloHost siloHost, ILogger logger)
        {
            await siloHost.StartAsync();
            logger.LogInformation("Silo started.");
        }

        private static async Task StopSilo(ISiloHost siloHost, ILogger logger)
        {
            await siloHost.StopAsync();
            SiloStopped.Set();
            logger.LogInformation("Silo stopped.");
        }
    }
}