using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuClear.Broadway.Interfaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NuClear.Broadway.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton(CreateClusterClient);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
        
        private static IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "Broadway";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ICampaignGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddSerilog())
                .Build();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Startup>(); 

            StartClientWithRetries(logger, client).Wait();
            
            return client;
        }

        private static async Task StartClientWithRetries(
            ILogger logger, 
            IClusterClient client,
            int initializeAttemptsBeforeFailing = 5)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    await client.Connect();
                    logger.LogInformation("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException ex)
                {
                    attempt++;
                    logger.LogWarning(
                        ex,
                        "Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.",
                        attempt,
                        initializeAttemptsBeforeFailing);
                    
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }
        }
    }
}