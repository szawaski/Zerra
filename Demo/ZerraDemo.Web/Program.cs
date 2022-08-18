using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Zerra;
using ZerraDemo.Common;

namespace ZerraDemo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Config.LoadConfiguration(args);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    _ = webBuilder.UseStartup<Startup>();
                });
    }
}
