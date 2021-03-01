using Azure.Identity;
using FirmaXadesNetCore;
using FormRecognizerFace.FormRecognizer;
using FormRecognizerFace.KeyVault;
using FormRecognizerFace.Storage;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

[assembly: FunctionsStartup(typeof(FormRecognizerFace.Fx.Startup))]
namespace FormRecognizerFace.Fx
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Load configuration from Azure App Configuration
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            var configuration = configurationBuilder
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                    .AddEnvironmentVariables() 
                                    .AddUserSecrets(Assembly.GetExecutingAssembly())
                                    .Build();

            // Get data from secrets
            var connectionString = configuration.GetConnectionString("AppConfiguration"); 
#if DEBUG
            var keyVaultCredential = new AzureCliCredential();
#else 
            var keyVaultCredential = new DefaultAzureCredential();
#endif
            // Create a new configurationbuilder and add appconfiguration
            configuration = configurationBuilder.AddAzureAppConfiguration(options =>
            { 
                options.Connect(connectionString) 
                       .Select(KeyFilter.Any, LabelFilter.Null)                         
                       // We can connect to Azure Key Vault using credentials 
                       .ConfigureKeyVault(kv =>
                       {
                           kv.SetCredential(keyVaultCredential);
                       });
            }).Build();

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddSingleton<IKeyVaultService>(s => new KeyVaultService(configuration["AppSettings:KeyVault:Name"]));
            builder.Services.Configure<BlobStorageRepositoryOptions>(configuration.GetSection("BlobStorageRepositoryOptions"));
            builder.Services.AddSingleton<IBlobStorageRepository, BlobStorageRepository>();
            builder.Services.Configure<FormRecognizerServiceOptions>(configuration.GetSection("FormRecognizerServiceOptions"));
            builder.Services.AddSingleton<IFormRecognizerService, FormRecognizerService>();
            builder.Services.AddSingleton<XadesService>();
           
        }         
    }
}
