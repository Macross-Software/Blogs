using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApplication.Pages
{
	public class IndexModel : PageModel
	{
		private static readonly HttpClient s_Client = new HttpClient();

		private readonly ILogger<IndexModel> _Logger;
		private readonly IOptions<ApiOptions> _Options;

		[Required]
		[BindProperty]
		public string? SearchQuery { get; set; }

		public string? SearchResponse { get; set; }

		public IndexModel(ILogger<IndexModel> logger, IOptions<ApiOptions> options)
		{
			_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public void OnGet() => _Logger.LogInformation("IndexModel.OnGet");

		public async Task OnPostAsync()
		{
			_Logger.LogInformation("BEGIN IndexModel.OnPostAsync");

			SearchResponse = await Search(SearchQuery).ConfigureAwait(false);

			_Logger.LogInformation("END IndexModel.OnPostAsync");
		}

		private async Task<string> Search(string query)
		{
			using HttpRequestMessage request = new HttpRequestMessage(
				HttpMethod.Get,
				$"{new Uri(_Options.Value.ServiceUrl, "search")}?query={HttpUtility.UrlEncode(query)}");

			using HttpResponseMessage response = await s_Client.SendAsync(request).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		}
	}
}
