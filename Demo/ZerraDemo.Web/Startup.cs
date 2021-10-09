using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zerra.Web;
using ZerraDemo.Web.Components;

namespace ZerraDemo.Web
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            HostEnvironment = env;
        }

        public IWebHostEnvironment HostEnvironment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var mvc = services.AddControllersWithViews();

            if (HostEnvironment.IsDevelopment())
            {
                mvc.AddRazorRuntimeCompilation();
            }
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddZerraLogger();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCustomAuthentication();

            app.UseCQRSGateway();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
