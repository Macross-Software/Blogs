using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebService
{
	public static class Program
	{
		public static void Main(string[] args)
			=> CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
#if WINDOWS && DEBUG
				.ConfigureDebugWindow()
#endif
				.ConfigureLogging((hostbuilder, loggingBuilder) => loggingBuilder
					.AddFiles(options =>
					{
						options.ApplicationName = hostbuilder.HostingEnvironment.ApplicationName;
						options.IncludeGroupNameInFileName = true;
					}))
				.ConfigureWebHostDefaults(webBuilder
					=> webBuilder.UseStartup<Startup>());
	}
}
