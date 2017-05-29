using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConsulRx.Configuration.TestHarness.Controllers
{
    public class ConfigurationController : Controller
    {
        private readonly IConfiguration _config;

        public ConfigurationController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok("Hello");
        }

        [HttpGet("configuration")]
        public IActionResult Index()
        {
            var allSettings = _config.AsEnumerable()
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .OrderBy(p => p.Key)
                .ToDictionary(p => p.Key, p => p.Value);
            
            return Json(allSettings);
        }
    }
}