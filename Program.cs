using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Syncthing
{
    public class Program2
    {
        public static void Main2(string[] args)
        {
            // CreateHostBuilder(args).Build().Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseSerilog((hostBuilderContext, services, options) =>
            {
                options.MinimumLevel.Information();
                options.WriteTo.File("logs/debug.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: null);
            })
            .ConfigureServices((hostBuilderContext, services) =>
            {
                services.AddSingleton<EndPointFactory>();
                services.AddHostedService<SyncService>();
            });
    }
}
