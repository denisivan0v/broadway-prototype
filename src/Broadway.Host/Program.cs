using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace NuClear.Broadway.Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration(
                       (context, builder) =>
                           {
                               builder.Sources.Clear();

                               var env = context.HostingEnvironment;
                               builder.SetBasePath(env.ContentRootPath)
                                      .AddJsonFile("appsettings.json")
                                      .AddJsonFile($"appsettings.{env.EnvironmentName?.ToLower()}.json")
                                      .AddEnvironmentVariables("ROADS_");
                           })
                   .UseStartup<Startup>()
                   .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
                   .Build();
    }
}