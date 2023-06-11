using ElectronNET.API;
using OpenStreetMap.API;
using OpenStreetMap.Data.Download;

namespace Funtlas.UI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<OverpassApiProvider>();
            services.AddSingleton<MapDownloadProvider>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            _ = Task.Run(async () =>
            {
                await Electron.WindowManager.CreateWindowAsync();
            });
        }
    }
}
