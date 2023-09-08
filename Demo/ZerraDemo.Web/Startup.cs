using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.Settings;
using Zerra.Logger;
using Zerra.Web;
using ZerraDemo.Common;
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
                _ = mvc.AddRazorRuntimeCompilation();
            }
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            _ = loggerFactory.AddZerraLogger();

            _ = app.UseHttpsRedirection();
            _ = app.UseStaticFiles();

            _ = app.UseCustomAuthentication();

            ServiceManager.StartServices();

            _ = app.UseCQRSGateway();

            _ = app.UseRouting();

            _ = app.UseAuthorization();

            _ = app.UseEndpoints(endpoints =>
            {
                _ = endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
