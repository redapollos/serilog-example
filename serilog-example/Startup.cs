using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RainstormTech.Core.Components.Logging;
using Serilog;
using serilog_example.Data;

namespace RainstormTech
{
    public class Startup
    {

        public static IWebHostEnvironment AppEnvironment { get; private set; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            AppEnvironment = env;

            // define serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.With<CommonEventEnricher>()
                .CreateLogger();

            // debug issues with serilog itself
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // add appsettings availability
            services.AddSingleton(Configuration);

            // define SQL Server connection string
            services.AddDbContext<ApplicationContext>(options =>
                options
                    .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    );

            // ability to grab httpcontext
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddMvc();

            // here's are serilog enricher 
            services.AddTransient<CommonEventEnricher>();
        }

        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRouting();

            // will use default cors policy
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            // captures extra variables from the httpContext if we want
            app.UseSerilogRequestLogging(options =>
            {
                // Emit debug-level events instead of the defaults
                // options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    // if you are using authentication, you can grab their userid/username
                    // diagnosticContext.Set("UserId", httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value.ToGuid() ?? null);

                    // get client's ip
                    diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress); 
                };
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
