using System.IO;
using Microsoft.AspNetCore.Mvc;
using PortfolioPicker.App;

namespace PortfolioPicker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BalanceController : Controller
    {
        [HttpPost]
        public Portfolio Post()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var yaml = reader.ReadToEnd();
                var original = Portfolio.FromYaml(yaml);
                return Picker.Rebalance(
                    original,
                    stockRatio: .9,
                    domesticStockRatio: .6,
                    domesticBondRatio: .7);
            }
        }
    }
}