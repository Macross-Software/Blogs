using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebApplication
{
	public class Startup
	{
		private readonly IConfiguration _Configuration;

		public Startup(IConfiguration configuration)
		{
			_Configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRazorPages();

			services.Configure<ApiOptions>(options => _Configuration.GetSection("Api").Bind(options));

			services.AddOpenTelemetry(builder => builder
				.SetResource(new Resource(new Dictionary<string, object>
				{
					[Resource.ServiceNameKey] = "WebApp",
					["service.datacenterId"] = _Configuration.GetValue<int?>("DatacenterId") ?? 0
				}))
				.AddAspNetCoreInstrumentation()
				.AddHttpInstrumentation()
				.UseJaegerExporter());
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if ((_Configuration.GetValue<bool?>("UseDeveloperExceptionPage") ?? true) && env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => endpoints.MapRazorPages());
		}
	}
}
