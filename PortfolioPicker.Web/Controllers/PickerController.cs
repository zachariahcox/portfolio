using System.Collections.Generic;
using System.Linq;
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
                    AccountType=AccountType.ROTH,
                    Taxable=false,
                    Value=100
                },
            };

            var p = new Picker(accounts, "FourFundStrategy");
            return p.Pick();
        }
    }
}