using System;
using System.Net;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace NuClear.Broadway.Silo
{
    internal class Program
    {
        private static readonly ManualResetEvent SiloStopped = new ManualResetEvent(false);
        
        private static ISiloHost _siloHost;

        private static void Main(string[] args)
        {
            _siloHost = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "orleans-docker";
                    options.ServiceId = "AspNetSampleApp";
                })
                .UseLocalhostClustering(clusterId: "dev")
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(CampaignGrain).Assembly).WithReferences())
                .AddMemoryGrainStorageAsDefault()
                .AddLogStorageBasedLogConsistencyProviderAsDefault()
                .AddStateStorageBasedLogConsistencyProviderAsDefault()
                .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Warning).AddConsole())
                .Build();

            Task.Run(StartSilo);

            AssemblyLoadContext.Default.Unloading += context =>
            {
                Task.Run(StopSilo);
                SiloStopped.WaitOne();
            };

            SiloStopped.WaitOne();
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