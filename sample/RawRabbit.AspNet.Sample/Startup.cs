using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RawRabbit.AspNet.Sample.Controllers;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Instantiation;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace RawRabbit.AspNet.Sample
{
	public class Startup
	{
		private readonly string _rootPath;

		public Startup(IHostingEnvironment env)
		{
			this._rootPath = env.ContentRootPath;
			IConfigurationBuilder builder = new ConfigurationBuilder()
				.SetBasePath(this._rootPath)
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables();
			this.Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddRawRabbit(new RawRabbitOptions
					{
						ClientConfiguration = this.GetRawRabbitConfiguration(),
						Plugins = p => p
							.UseStateMachine()
							.UseGlobalExecutionId()
							.UseHttpContext()
							.UseMessageContext(c => new MessageContext
							{
								Source = c.GetHttpContext().Request.GetDisplayUrl()
							})
					})
				.AddMvc();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			Log.Logger = this.GetConfiguredSerilogger();
			loggerFactory
				.AddSerilog()
				.AddConsole(this.Configuration.GetSection("Logging"));

			app.UseMvc();
		}

		private ILogger GetConfiguredSerilogger()
		{
			return new LoggerConfiguration()
				.WriteTo.File($"{this._rootPath}/Logs/serilog.log", LogEventLevel.Debug)
				.WriteTo.LiterateConsole()
				.CreateLogger();
		}

		private RawRabbitConfiguration GetRawRabbitConfiguration()
		{
			IConfigurationSection section = this.Configuration.GetSection("RawRabbit");
			if (!section.GetChildren().Any())
			{
				throw new ArgumentException("Unable to configuration section 'RawRabbit'. Make sure it exists in the provided configuration");
			}
			return section.Get<RawRabbitConfiguration>();
		}
	}
}
