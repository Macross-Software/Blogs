using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebService.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SearchController : ControllerBase
	{
		private static readonly HttpClient s_Client = new HttpClient();

		private readonly ILogger<SearchController> _Logger;
		private readonly IOptions<DatabaseOptions> _Options;

		public SearchController(ILogger<SearchController> logger, IOptions<DatabaseOptions> options)
		{
			_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		[HttpGet]
		public async Task<IActionResult> Search([FromQuery] string query)
		{
			_Logger.WriteInfo(
				new
				{
					Query = query
				},
				"Search executing.");

			try
			{
				await WriteQueryToSql(query).ConfigureAwait(false);

				await SearchGoogle(query).ConfigureAwait(false);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				_Logger.WriteError(ex, "Unhandled Exception thrown.");
				return Problem(ex.Message);
			}

			return new EmptyResult();
		}

		private async Task WriteQueryToSql(string query)
		{
			using SqlConnection connection = new SqlConnection(_Options.Value.ConnectionString);

			await connection.OpenAsync().ConfigureAwait(false);

			using SqlCommand command = new SqlCommand("sp_StoreQuery", connection)
			{
				CommandType = CommandType.StoredProcedure
			};

			command.Parameters.Add("@Query", SqlDbType.NVarChar).Value = query;

			await command.ExecuteNonQueryAsync().ConfigureAwait(false);
		}

		private async Task SearchGoogle(string query)
		{
			using HttpRequestMessage request = new HttpRequestMessage(
				HttpMethod.Get,
				$"https://www.google.com?query={HttpUtility.UrlEncode(query)}");

			using HttpResponseMessage response = await s_Client.SendAsync(request).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

			Response.StatusCode = 200;

			await stream.CopyToAsync(Response.Body).ConfigureAwait(false);
		}
	}
}
