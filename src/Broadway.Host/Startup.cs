using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json.Linq;

using NuClear.Broadway.DataProjection;
using NuClear.Broadway.Host.Options;
using NuClear.Broadway.Interfaces.Grains;

using Orleans;
using Orleans.Clustering.Cassandra;
using Orleans.Configuration;
using Orleans.Runtime;

using Serilog;

using Swashbuckle.AspNetCore.Swagger;

using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NuClear.Broadway.Host
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
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

            var authenticationOptions = _configuration.GetSection("Authentication").Get<JwtAuthenticationOptions>();
            var certificate = Base64UrlEncoder.DecodeBytes(authenticationOptions.Certificate);
            var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = authenticationOptions.Issuer,
                    ValidateIssuer = true,
                    IssuerSigningKey = new X509SecurityKey(new X509Certificate2(certificate)),
                    ValidateIssuerSigningKey = true
                };

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(
                        options =>
                            {
                                options.Audience = authenticationOptions.Audience;
                                options.TokenValidationParameters = tokenValidationParameters;
                            });

            var connectionString = _configuration.GetConnectionString("BroadwayDataProjection");
            services.AddDbContextPool<DataProjectionContext>(builder => builder.UseNpgsql(connectionString));

            services.AddSwaggerGen(
                options =>
                    {
                        var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
                        foreach (var description in provider.ApiVersionDescriptions)
                        {
                            options.SwaggerDoc(
                                description.GroupName,
                                new Info { Title = $"Broadway API {description.ApiVersion}", Version = description.ApiVersion.ToString() });
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

                        options.AddSecurityRequirement(
                            new Dictionary<string, IEnumerable<string>>
                                {
                                    { "Bearer", new string[] { } }
                                });

                        // options.IncludeXmlComments(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nameof(Broadway)}.{nameof(Host)}.xml"));
                    });

            services.AddSingleton(CreateClusterClient);
        }

        public void Configure(IApplicationBuilder app)
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

                                    if (_environment.IsDevelopment())
                                    {
                                        error.Add("details", feature.Error.ToString());
                                    }

                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(new JObject(new JProperty("error", error)).ToString());
                                }
                    });

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location"));
            app.UseAuthentication();
            app.UseMvc();

            if (!_environment.IsProduction())
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
            var client = new ClientBuilder()
                         .UseEnvironment(_environment.EnvironmentName)
                         .ConfigureAppConfiguration(
                             builder =>
                                 {
                                     builder.Sources.Clear();

                                     var env = _environment.EnvironmentName;
                                     builder.SetBasePath(_environment.ContentRootPath)
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