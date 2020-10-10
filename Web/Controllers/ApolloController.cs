using System;
using System.Net;
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
            "Deelee",
            "ok",
            "yeah",
            "That's ok!"
        };

        [HttpGet]
        public ContentResult Get()
        {
            var dice = new Random();
            var myRandomNumber = dice.Next(minValue:0, maxValue:ThingsToSay.Length);
            var whatToSay = ThingsToSay[myRandomNumber];
            var html = MakeHtml(whatToSay);

            return new ContentResult {
                ContentType = "text/html",
                StatusCode = (int) HttpStatusCode.OK,
                Content = html
            };
        }

        private string MakeHtml(string whatToSay)
        {
            return @"<!DOCTYPE html>
<html>
<head>
<style>
.rainbow-text {
    background-image: linear-gradient(to left, violet, indigo, blue, green, yellow, orange, red);
    -webkit-text-fill-color: transparent;
    -webkit-background-clip: text;
    background-clip: text;
    font-size: 25px;
    transition: background-image .25s ease-in-out;

    animation-name: spin;
    animation-duration: 5000ms;
    animation-iteration-count: infinite;
    animation-timing-function: linear; 
}
.center-screen {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  text-align: center;
  min-height: 100vh;
}
@keyframes spin {
    from {
        transform:rotate(0deg);
    }
    to {
        transform:rotate(360deg);
    }
}
</style>
</head>
<body>
    <div class='rainbow-text center-screen'>" + whatToSay + @"</div>
</body>
</html>
";
        }
    }
}
