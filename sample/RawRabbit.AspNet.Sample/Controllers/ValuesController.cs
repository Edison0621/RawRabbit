using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RawRabbit.Messages.Sample;
using RawRabbit.Operations.MessageSequence;
using RawRabbit.Operations.MessageSequence.Model;

namespace RawRabbit.AspNet.Sample.Controllers;

public class ValuesController : Controller
{
	private readonly IBusClient _busClient;
	private readonly Random _random;
	private readonly ILogger<ValuesController> _logger;

	public ValuesController(IBusClient legacyBusClient, ILoggerFactory loggerFactory)
	{
		this._busClient = legacyBusClient;
		this._logger = loggerFactory.CreateLogger<ValuesController>();
		this._random = new Random();
	}

	[HttpGet]
	[Route("api/values")]
	public async Task<IActionResult> GetAsync()
	{
		this._logger.LogDebug("Received Value Request.");
		MessageSequence<ValuesCalculated> valueSequence = this._busClient.ExecuteSequence(s => s
			.PublishAsync(new ValuesRequested
			{
				NumberOfValues = this._random.Next(1,10)
			})
			.When<ValueCreationFailed, MessageContext>(
				(failed, context) =>
				{
					this._logger.LogWarning("Unable to create Values. Exception: {0}", failed.Exception);
					return Task.FromResult(true);
				}, it => it.AbortsExecution())
			.Complete<ValuesCalculated>()
		);

		try
		{
			await valueSequence.Task;
		}
		catch (Exception e)
		{
			return this.StatusCode((int)HttpStatusCode.InternalServerError, $"No response received. Is the Console App started? \n\nException: {e}");
		}

		this._logger.LogInformation("Successfully created {valueCount} values", valueSequence.Task.Result.Values.Count);

		return this.Ok(valueSequence.Task.Result.Values);
	}

	[HttpGet("api/values/{id}")]
	public async Task<string> GetAsync(int id)
	{
		this._logger.LogInformation("Requesting Value with id {valueId}", id);
		ValueResponse response = await this._busClient.RequestAsync<ValueRequest, ValueResponse>(new ValueRequest {Value = id});
		return response.Value;
	}
}
