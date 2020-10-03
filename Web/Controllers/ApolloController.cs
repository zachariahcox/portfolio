using System;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApolloController : ControllerBase
    {
        private static readonly string[] ThingsToSay = new[]
        {
            "Abbidy dupe dap!", 
            "I'm hungry",
            "I'm not sure?",
            "I'm tired",
            "Apps",
            "Deelee"
        };

        [HttpGet]
        public string Get()
        {
            var randomNumberGenerator = new Random();
            return ThingsToSay[randomNumberGenerator.Next(0, ThingsToSay.Length)];
        }
    }
}
