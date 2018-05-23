﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NuClear.Broadway.Interfaces.Grains;

using Orleans;
using Orleans.Clustering.Cassandra;
using Orleans.Configuration;

using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NuClear.Broadway.TaskRunner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ROADS_ENVIRONMENT") ?? "Production";
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(basePath)
                                .AddJsonFile("appsettings.json")
                                .AddJsonFile($"appsettings.{env.ToLower()}.json")
                                .AddEnvironmentVariables("ROADS_")
                                .Build();

            var serilogLogger = CreateLogger(configuration);

            var serviceProvider = new ServiceCollection()
                .AddLogging(x => x.AddSerilog(serilogLogger, true))
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

            var cts = new GrainCancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
                                          {
                                              logger.LogInformation("Application is shutting down...");
                                              cts.Cancel();
                                              serviceProvider.Dispose();

                                              eventArgs.Cancel = true;
                                          };

            var app = new CommandLineApplication { Name = "Broadway.Worker" };
            app.HelpOption(CommandLine.HelpOptionTemplate);
            app.OnExecute(
                () =>
                    {
                        Console.WriteLine("Broadway worker host.");
                        app.ShowHelp();

                        return 0;
                    });

            var clusterClient = CreateClusterClient(env, basePath, logger, serilogLogger);

            app.Command(
                CommandLine.Commands.Import,
                config =>
                    {
                        config.Description = "Run import worker. See available arguments for details.";
                        config.HelpOption(CommandLine.HelpOptionTemplate);
                        config.Command(
                            CommandLine.CommandTypes.FlowCardsForERM,
                            commandConfig =>
                                {
                                    commandConfig.Description = "Import objects from FlowCardsForERM flow.";
                                    commandConfig.HelpOption(CommandLine.HelpOptionTemplate);
                                    commandConfig.OnExecute(() => Run(commandConfig, logger, clusterClient, cts));
                                });

                        config.Command(
                            CommandLine.CommandTypes.FlowKaleidoscope,
                            commandConfig =>
                                {
                                    commandConfig.Description = "Import objects from FlowKaleidoscope flow.";
                                    commandConfig.HelpOption(CommandLine.HelpOptionTemplate);
                                    commandConfig.OnExecute(() => Run(commandConfig, logger, clusterClient, cts));
                                });

                        config.OnExecute(
                            () =>
                                {
                                    config.ShowHelp();

                                    return 0;
                                });
                    });

            var exitCode = 0;
            try
            {
                logger.LogInformation("Broadway Worker started with options: {workerOptions}.", args.Length != 0 ? string.Join(" ", args) : "N/A");
                exitCode = app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                ex.Command.ShowHelp();
                exitCode = 1;
            }
            catch (WorkerGrainNotFoundExeption)
            {
                exitCode = 2;
            }
            catch (Exception ex)
            {
                logger.LogCritical(default, ex, "Unexpected error occured. See logs for details.");
                exitCode = -1;
            }
            finally
            {
                logger.LogInformation("Broadway Worker is shutting down with code {workerExitCode}.", exitCode);
            }

            Environment.Exit(exitCode);
        }

        private static Serilog.ILogger CreateLogger(IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration);
            Log.Logger = loggerConfiguration.CreateLogger();

            return Log.Logger;
        }

        private static IClusterClient CreateClusterClient(string environmentName, string basePath, ILogger logger, Serilog.ILogger serilogLogger)
        {
            var client = new ClientBuilder()
                         .UseEnvironment(environmentName)
                         .ConfigureAppConfiguration(
                             builder =>
                                 {
                                     builder.Sources.Clear();

                                     var env = environmentName;
                                     builder.SetBasePath(basePath)
                                            .AddJsonFile("appsettings.json")
                                            .AddJsonFile($"appsettings.{env?.ToLower()}.json")
                                            .AddEnvironmentVariables("ROADS_");
                                 })
                         .Configure<ClusterOptions>(
                             options =>
                                 {
                                     options.ClusterId = "broadway-prototype";
                                     options.ServiceId = "broadway";
                                 })
                         .UseCassandraGatewayListProvider(config => config.GetSection("Cassandra"))
                         .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ICampaignGrain).Assembly).WithReferences())
                         .ConfigureLogging(logging => logging.AddSerilog(serilogLogger))
                         .Build();

            StartClientWithRetries(logger, client).Wait();

            return client;
        }

        private static async Task StartClientWithRetries(
            ILogger logger,
            IClusterClient client)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    await client.Connect(
                        async ex =>
                            {
                                attempt++;
                                logger.LogWarning("Attempt {attempt} failed to initialize the Orleans client.", attempt);

                                await Task.Delay(TimeSpan.FromSeconds(2));

                                return true;
                            });

                    logger.LogInformation("Client successfully connect to silo host.");

                    break;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Failed to initialize the Orleans client.");

                    throw;
                }
            }
        }

        private static int Run(CommandLineApplication app, ILogger logger, IClusterClient clusterClient, GrainCancellationTokenSource cts)
        {
            var registry = new WorkerGrainRegistry(logger, clusterClient);

            var taskId = app.Parent.Name;
            var taskType = app.Name;
            var workerGrain = registry.GetWorkerGrain(taskId, taskType);

            logger.LogInformation(
                "Starting worker of type {workerType} with identity {workerIndentity}...",
                workerGrain.GetType().Name,
                workerGrain.GetGrainIdentity());

            workerGrain.InvokeOneWay(x => x.StartExecutingAsync(cts.Token));

            logger.LogInformation(
                "Worker of type {workerType} with identity {workerIndentity} completed successfully.",
                workerGrain.GetType().Name,
                workerGrain.GetGrainIdentity());

            return 0;
        }
    }
}