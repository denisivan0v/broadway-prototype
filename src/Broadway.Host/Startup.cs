using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuClear.Broadway.Interfaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Hosting;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NuClear.Broadway.Host
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvcCore()
                .AddVersionedApiExplorer()
                .AddApiExplorer()
                .AddAuthorization()
                .AddCors()
                .AddJsonFormatters();

            services.AddApiVersioning(options => options.ReportApiVersions = true);

            services.AddSwaggerGen(
                options =>
                {
                    var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerDoc(description.GroupName, new Info { Title = $"Broadway API {description.ApiVersion}", Version = description.ApiVersion.ToString() });
                    }

                    options.AddSecurityDefinition(
                        "Bearer",
                        new ApiKeyScheme
                        {
                            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                            Name = "Authorization",
                            In = "header",
                            Type = "apiKey"
                        });

                    options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", new string[] { } }
                    });

                    options.IncludeXmlComments(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nameof(Broadway)}.{nameof(Host)}.xml"));
                });

            services.AddSingleton(CreateClusterClient);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler(
                new ExceptionHandlerOptions
                {
                    ExceptionHandler =
                        async context =>
                        {
                            var feature = context.Features.Get<IExceptionHandlerFeature>();
                            var error = new JObject
                            {
                                { "requestId", context.TraceIdentifier },
                                { "code", "unhandledException" },
                                { "message", feature.Error.Message }
                            };

                            if (env.IsDevelopment())
                            {
                                error.Add("details", feature.Error.ToString());
                            }

                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(new JObject(new JProperty("error", error)).ToString());
                        }
                });

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location"));

            app.UseMvc();

            if (!env.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(
                    options =>
                    {
                        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
                        foreach (var description in provider.ApiVersionDescriptions)
                        {
                            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                        }

                        options.DocExpansion(DocExpansion.None);
                        options.EnableValidator();
                        options.ShowExtensions();
                        options.DisplayRequestDuration();
                    });
            }
        }

        private IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            const string Invariant = "Npgsql";

            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "broadway-prototype";
                    options.ServiceId = "broadway";
                })
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = _configuration.GetConnectionString("Orleans");
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ICampaignGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddSerilog(Log.Logger))
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