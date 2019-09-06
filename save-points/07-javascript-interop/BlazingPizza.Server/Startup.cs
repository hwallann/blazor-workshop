using System;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
namespace BlazingPizza.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddNewtonsoftJson();

            services.AddDbContext<PizzaStoreContext>(options => options.UseSqlite("Data Source=pizza.db"));

            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { MediaTypeNames.Application.Octet });
            });
            
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = "e143a0a2-9c72-40b4-9bc5-f4b5ceb50ccb";
                    microsoftOptions.ClientSecret = "]CUIpyZV0g7DTp-gRg+[jItHMTlWC7C8";
                    microsoftOptions.Events.OnRemoteFailure = (context) =>
                    {
                        context.HandleResponse();
                        return context.Response.WriteAsync("<script>window.close();</script>");
                    };
                    microsoftOptions.Scope.Clear();
                    microsoftOptions.Scope.Add("https://graph.microsoft.com/user.read");
                    microsoftOptions.Scope.Add("https://graph.microsoft.com/mail.read");
                    Console.WriteLine("****************************************");
                    Console.WriteLine("****************************************");
                    foreach(var item in microsoftOptions.Scope)
                        Console.WriteLine(item);
                    Console.WriteLine("****************************************");
                    Console.WriteLine("****************************************");
                });
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseClientSideBlazorFiles<Client.Startup>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToClientSideBlazor<Client.Startup>("index.html");
            });
        }
    }
}
