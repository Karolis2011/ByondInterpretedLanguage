using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ByondLang
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            if (args.Contains("--worker"))
            {
                CreateWorkerHostBuilder(args).Build().Run();
                return;
            }
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options => {
                        options.Limits.MaxRequestLineSize = (int)Math.Pow(2, 16);
                    });
                    webBuilder.UseUrls("http://localhost:1945");
                    webBuilder.UseStartup<Startup>();
                });

        public static IHostBuilder CreateWorkerHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        options.Limits.MaxRequestLineSize = (int)Math.Pow(2, 16);
                    });
                    webBuilder.UseStartup<WorkerStartup>();
                })
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        var port = Convert.ToInt32( Environment.GetEnvironmentVariable("_WORKER_PORT"));
                        options.ListenLocalhost(port, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
                    });
                });
    }
}
