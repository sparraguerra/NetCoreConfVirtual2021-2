using FormRecognizerFace.CosmosDb;
using FormRecognizerFace.Storage;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace FormRecognizerFace.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();


            services.AddOptions();
            services.AddHttpContextAccessor();

            services.Configure<BlobStorageRepositoryOptions>(Configuration.GetSection("BlobStorageRepositoryOptions"));
            services.AddSingleton<IBlobStorageRepository, BlobStorageRepository>();

            services.Configure<CosmosDbRepositoryOptions>(Configuration.GetSection("CosmosDbRepositoryOptions"));
            services.AddSingleton<ICosmosDbClientFactory>(s =>
            {
                var configuration = s.GetService<IOptions<CosmosDbRepositoryOptions>>();
                return new CosmosDbClientFactory(configuration);
            });
            
            services.AddSingleton<ICosmosDbRepository<Company>, FormRecognizerCosmosDbRepository>(s =>
            {
                var factory = s.GetService<ICosmosDbClientFactory>();
                return new FormRecognizerCosmosDbRepository(factory);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
