using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

            var p = Picker.Create(accounts, "FourFundStrategy");
            return p.Pick();
        }

        [HttpPost]
        public async Task<Portfolio> Post()
        {
            var file = Request.Form.Files[0];
            if (file.Length > MAX_FILE_SIZE)
            {
                throw new Exception("file too big");
            }

            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                var data = await reader.ReadToEndAsync();
                var p = Picker.Create(data, "FourFundStrategy");
                return p.Pick();
            }
        }

        private const int MAX_FILE_SIZE = 1024 * 1024; // 1Mb
    }
}