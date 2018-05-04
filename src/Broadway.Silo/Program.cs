using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Grains;
using NuClear.Broadway.Grains.Options;
using NuClear.Broadway.Kafka;
using Orleans;
using Orleans.Clustering.Cassandra;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Extensions.Logging;

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
            const string Invariant = "Npgsql";

            var env = Environment.GetEnvironmentVariable("ROADS_ENVIRONMENT") ?? "Production";
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Serilog.ILogger logger = null;
            string connectionString = null;

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
                               logger = CreateSerilogLogger(context.Configuration);
                               connectionString = context.Configuration.GetConnectionString("Orleans");

                               var kafkaOptions = context.Configuration.GetSection("Kafka").Get<KafkaOptions>();
                               var referenceObjectsClusterKafkaOptions =
                                   context.Configuration.GetSection("ReferenceObjectsKafkaCluster").Get<ReferenceObjectsClusterKafkaOptions>();

                               services.AddSingleton(kafkaOptions.MergeWith(referenceObjectsClusterKafkaOptions));
                           })
                   .Configure<ClusterOptions>(
                       options =>
                           {
                               options.ClusterId = "broadway-prototype";
                               options.ServiceId = "broadway";
                           })
                   .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                   .UseCassandraClustering(
                       options =>
                           {
                               options.ContactPoints = new[] { "localhost" };
                               options.ReplicationFactor = 1;
                           },
                       new SerilogLoggerProvider(logger))
                   .AddAdoNetGrainStorageAsDefault(
                       options =>
                           {
                               options.Invariant = Invariant;
                               options.ConnectionString = connectionString;
                               options.UseJsonFormat = true;
                           })
                   .AddLogStorageBasedLogConsistencyProviderAsDefault()
                   .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CampaignGrain).Assembly).WithReferences())
                   .ConfigureLogging(logging => logging.AddSerilog(logger, true))
                   .AddIncomingGrainCallFilter<StateModificationCallFilter>()
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