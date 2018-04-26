using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Grains;
using NuClear.Broadway.Kafka;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using ILogger = Serilog.ILogger;

namespace NuClear.Broadway.Silo
{
    internal class Program
    {
        private static readonly ManualResetEvent SiloStopped = new ManualResetEvent(false);
        
        private static ISiloHost _siloHost;

        private static void Main(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ROADS_ENVIRONMENT") ?? "Production";
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env?.ToLower()}.json")
                .AddEnvironmentVariables("ROADS_")
                .Build();
            
            const string invariant = "Npgsql";
            var connectionString = configuration.GetConnectionString("Orleans");

            _siloHost = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "broadway-prototype";
                    options.ServiceId = "broadway";
                })
                .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = invariant;
                    options.ConnectionString = connectionString;
                })
                .AddAdoNetGrainStorageAsDefault(
                    options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = connectionString;
                        options.UseJsonFormat = true;
                    })
                .AddLogStorageBasedLogConsistencyProviderAsDefault()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CampaignGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddSerilog(CreateLogger(configuration), true))
                .ConfigureServices((context, services) =>
                {
                    var kafkaOptions = configuration.GetSection("Kafka").Get<KafkaOptions>();
                    services.AddSingleton(kafkaOptions);
                })
                .Build();

            Task.Run(StartSilo);

            AssemblyLoadContext.Default.Unloading += context =>
            {
                Task.Run(StopSilo);
                SiloStopped.WaitOne();
            };

            SiloStopped.WaitOne();
        }

        private static ILogger CreateLogger(IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration);
            Log.Logger = loggerConfiguration.CreateLogger();

            return Log.Logger;
        }

        private static async Task StartSilo()
        {
            await _siloHost.StartAsync();
            Console.WriteLine("Silo started");
        }

        private static async Task StopSilo()
        {
            await _siloHost.StopAsync();
            Console.WriteLine("Silo stopped");
            SiloStopped.Set();
        }
    }
}