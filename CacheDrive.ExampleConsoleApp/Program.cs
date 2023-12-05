using CacheDrive.ExampleConsoleApp;
using CacheDrive.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

services.RegisterCacheDrive(configuration, configurationSectionName: "CacheSettings");
    
services.AddTransient<App>();

// create service provider
var serviceProvider = services.BuildServiceProvider();

await serviceProvider.GetService<App>()!.Run();

serviceProvider.Dispose();