using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using PortfolioPicker.App;

namespace PortfolioPicker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PickerController : ControllerBase
    {
        [HttpGet]
        public Portfolio Get()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="roth",
                    Brokerage="Vanguard",
                    Type=AccountType.ROTH,
                    Taxable=false,
                    Value=100
                },
            };

            var p = Picker.Create(accounts, "FourFundStrategy");
            return p.Pick();
        }

        [HttpPost]
        public Portfolio Post([FromBody] JToken token)
        {
            var p = Picker.Create(token.ToString(), "FourFundStrategy");
            return p.Pick();
        }

        private const int MAX_FILE_SIZE = 1024 * 1024; // 1Mb
    }
}