using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
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
        public Portfolio Post()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var yaml = reader.ReadToEnd();
                var p = Picker.Create(yaml, "FourFundStrategy");
                return p.Pick();
            }
        }
    }
}