using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.GoogleCloudLogging;
using System;
using System.IO;
using Serilog.Filters;
using DfT.DTRO.Extensions.Configuration;

namespace DfT.DTRO;

/// <summary>
/// Program
/// </summary>
public class Program
{
    /// <summary>
    /// Main
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// Create the web host builder.
    /// </summary>
    /// <param name="args"></param>
    /// <returns>IWebHostBuilder</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: false);
                }
                config.AddEnvironmentVariables();
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.Local.json", optional: true, reloadOnChange: false);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                .UseKestrel(options => options.AddServerHeader = false);
            });
    }
}