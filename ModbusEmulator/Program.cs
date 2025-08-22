using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using ModbusEmulator;
using Microsoft.Extensions.Logging.Console;

var builder = Host.CreateApplicationBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").Build();

builder.Services.AddOptions<EmulatorOptions>().BindConfiguration(nameof(EmulatorOptions)); 
builder.Services.AddLogging(logging =>
{
    logging.AddConsole(options =>
    {
        options.FormatterName = "CustomFormatter";
    });
    logging.AddDebug();
    logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
});

builder.Services.AddHostedService<ModbusPortEmulator>();

var host = builder.Build();

await host.RunAsync();