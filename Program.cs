using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

namespace DigitalTwinApi {
    public class Program {
        public static void Main (string[] args) {
            Console.WriteLine("DEVICE_NAME: " + Constants.DEVICE_NAME);
            CreateHostBuilder (args).Build ().Run ();
        }

        public static IHostBuilder CreateHostBuilder (string[] args) =>
            Host.CreateDefaultBuilder (args)
            .ConfigureAppConfiguration((context, config) => {
                if (context.HostingEnvironment.IsProduction()) {
                    var builtConfig = config.Build();
                    var secretClientApi = new SecretClient(new Uri($"https://{builtConfig["KeyVaultApi"]}.vault.azure.cn/"), new DefaultAzureCredential());
                    config.AddAzureKeyVault(secretClientApi, new KeyVaultSecretManager());
                    //var secretClientBlob = new SecretClient(new Uri($"https://{builtConfig["KeyVaultBlob"]}.vault.azure.net/"), new DefaultAzureCredential());
                    //config.AddAzureKeyVault(secretClientBlob, new KeyVaultSecretManager());
                }
            })
            .ConfigureWebHostDefaults (webBuilder => {
                webBuilder.UseStartup<Startup> ();
            });
    }
}