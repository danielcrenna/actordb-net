using System.Threading.Tasks;
using ActorDb.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActorDb.Api.Controllers
{
	public class ConfigurationController : Controller
	{
		private readonly IOptions<ActorDbSettings> _settings;
		private readonly ILogger<ActorDbClient> _logger;

		public ConfigurationController(IOptions<ActorDbSettings> settings, ILogger<ActorDbClient> logger)
		{
			_settings = settings;
			_logger = logger;
		}

		[HttpPut("login")]
		public async Task<IActionResult> LoginAsync([FromBody] LoginModel model)
		{
			using (var client = new ActorDbClient(_settings.Value.Host, _settings.Value.Port, _logger))
			{
				if (!await client.LoginSecureAsync(model.Username, model.Password))
					return Forbid();
				return Ok();
			}
		}

		[HttpPut("config")]
		public async Task<IActionResult> GetConfigurationAsync([FromBody] LoginModel model)
		{
			using (var client = new ActorDbClient(_settings.Value.Host, _settings.Value.Port, _logger))
			{
				if (!await client.LoginSecureAsync(model.Username, model.Password))
					return Forbid();

				var configuration = await client.GetConfigurationAsync();

				return Ok(configuration);
			}
		}

		[HttpGet("version")]
		public async Task<IActionResult> GetVersionAsync()
		{
			using (var client = new ActorDbClient(_settings.Value.Host, _settings.Value.Port, _logger))
			{
				var version = await client.GetProtocolVersionAsync();

				return Ok(new { Version = version });
			}
		}
	}
}