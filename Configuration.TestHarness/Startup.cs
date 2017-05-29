using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ConsulRx.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace ConsulRx.Configuration.TestHarness
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddConsul(consul =>
                {
                    consul
                        .MapHttpService("service1-http", "serviceEndpoints:service1")
                        .MapHttpService("service2-http", "serviceEndpoints:service2")
                        .MapKeyPrefix("apps/harness", "consul")
                        .MapKey("shared/feature1", "features:feature1");
                });
            Configuration = builder.Build();
        }

        protected Startup()
        {
        }

        public IConfigurationRoot Configuration { get; protected set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<IConfiguration>(Configuration);
        }

        public void Configure(IApplicationBuilder builder)
        {
            builder.UseMvc();
        }
    }
}