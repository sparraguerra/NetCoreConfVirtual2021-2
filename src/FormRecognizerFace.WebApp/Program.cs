using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace FormRecognizerFace.WebApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        var configuration = config.Build();

                        // Get data from secrets
                        var connectionString = configuration.GetConnectionString("AppConfiguration");

#if DEBUG 
                        var keyVaultCredential = new AzureCliCredential();
#else 
                        var keyVaultCredential = new DefaultAzureCredential();
#endif

                        config.AddAzureAppConfiguration(options =>
                        {
                            // We can connect to Azure AppConfiguration using a connection string or an uri with credentials
                            options.Connect(connectionString)
                                .Select(KeyFilter.Any, LabelFilter.Null)                                
                                // We can connect to Azure Key Vault using credentials 
                                .ConfigureKeyVault(kv =>
                                {
                                    kv.SetCredential(keyVaultCredential);
                                });
                        });
                    }).UseStartup<Startup>();
                });
    }
}
