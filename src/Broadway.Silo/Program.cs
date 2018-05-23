﻿using System;
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
using NuClear.Broadway.Interfaces.Workers;
using NuClear.Broadway.Kafka;
using NuClear.Broadway.Silo.Concurrency;

using Orleans;
using Orleans.Clustering.Cassandra;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence.Cassandra;
using Orleans.Runtime;

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

                               services.AddTransient<MessageSender>();

                               var connectionString = configuration.GetConnectionString("BroadwayDataProjection");
                               services.AddEntityFrameworkNpgsql()
                                       .AddDbContextPool<DataProjectionContext>(builder => builder.UseNpgsql(connectionString));
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
                   .AddStartupTask(
                       (serviceProvider, cancellationToken) =>
                           {
                               var taskScheduler = TaskScheduler.Current;
                               var tcs = new GrainCancellationTokenSource();
                               cancellationToken.Register(() => tcs.Cancel());

                               var grainFactory = serviceProvider.GetRequiredService<IGrainFactory>();

                               var dataProjectionGrain = grainFactory.GetGrain<IDataProjectionMakerGrain>(Guid.NewGuid().ToString());
                               Task.Factory.StartNew(
                                   async () =>
                                       {
                                           while (true)
                                           {
                                               try
                                               {
                                                   await dataProjectionGrain.StartExecutingAsync(tcs.Token);
                                               }
                                               catch (Exception ex)
                                               {
                                                   logger.Error(
                                                       ex,
                                                       "Unexpected error occured in worker {workerType}. Worker will be restarted.",
                                                       dataProjectionGrain.GetType().FullName);

                                                   await Task.Delay(1000, cancellationToken);
                                               }
                                           }
                                       },
                                   cancellationToken,
                                   TaskCreationOptions.LongRunning,
                                   taskScheduler);

                               return Task.CompletedTask;
                           })
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