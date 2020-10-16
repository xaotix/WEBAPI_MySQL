using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace daniel_api
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
        }
        //public void ConfigureServices(IServiceCollection services)
        //{
        //    // needed to load configuration from appsettings.json
        //    services.AddOptions();

        //    // needed to store rate limit counters and ip rules
        //    services.AddMemoryCache();

        //    //load general configuration from appsettings.json
        //    services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

        //    // inject counter and rules stores
        //    services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        //    services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

        //    // Add framework services.
        //    services.AddMvc();

        //    // https://github.com/aspnet/Hosting/issues/793
        //    // the IHttpContextAccessor service is not registered by default.
        //    // the clientId/clientIp resolvers use it.
        //    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        //    // configuration (resolvers, counter key builders)
        //    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        //}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
