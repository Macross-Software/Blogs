using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebService
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
			services.AddControllers();

			services.Configure<DatabaseOptions>(options => _Configuration.GetSection("Database").Bind(options));

			services.AddOpenTelemetry(builder => builder
				.SetResource(new Resource(new Dictionary<string, object>
				{
					[Resource.ServiceNameKey] = "WebService",
					["service.datacenterId"] = _Configuration.GetValue<int?>("DatacenterId") ?? 0
				}))
				.AddAspNetCoreInstrumentation()
				.AddHttpInstrumentation()
				.AddSqlClientDependencyInstrumentation()
				.UseJaegerExporter());
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if ((_Configuration.GetValue<bool?>("UseDeveloperExceptionPage") ?? true) && env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => endpoints.MapControllers());
		}
	}
}
