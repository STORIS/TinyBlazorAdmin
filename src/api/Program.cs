using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Cloud5mins.domain;
using Microsoft.Extensions.DependencyInjection;

namespace Cloud5mins.AdminApi
{
    public class Program
    {
        public static void Main()
        {
            AdminApiSettings AdminApiSettings = null;
            
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    // Add our global configuration instance
                    services.AddSingleton(options =>
                    {
                        var configuration = context.Configuration;
                        AdminApiSettings = new AdminApiSettings();
                        AdminApiSettings.version = typeof(Program).Assembly.GetName().Version.ToString();
                        configuration.Bind(AdminApiSettings);
                        return configuration;
                    });
            
                    // Add our configuration class
                    services.AddSingleton(options => { return AdminApiSettings; });
                })
                .Build();

            host.Run();
        }
    }
}