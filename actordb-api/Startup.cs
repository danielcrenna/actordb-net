using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActorDb.Api
{
    public class Startup
    {
	    public Startup(IConfiguration configuration, IHostingEnvironment env)
	    {
		    Configuration = configuration;
		    Environment = env;
	    }

	    public IConfiguration Configuration { get; }
	    public IHostingEnvironment Environment { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
        {
	        services.AddLogging(c => c.AddConsole());
	        services.Configure<ActorDbSettings>(Configuration.GetSection("ActorDb"));
	        services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

	        app.UseMvc();
        }
    }
}
