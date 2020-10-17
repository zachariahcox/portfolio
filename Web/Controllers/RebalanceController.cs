using Microsoft.AspNetCore.Mvc;
using PortfolioPicker.App;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;

namespace Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RebalanceController : ControllerBase
    {
        [HttpPost]
        public async Task<dynamic> RebalanceAsync()
        {
            try
            {
                // stream body into string
                var body = default(string);
                using (var reader = new StreamReader(this.Request.Body, Encoding.UTF8))
                    body = await reader.ReadToEndAsync();
                
                var gsp = GoogleSheetPortfolio.FromJson(body);
                if (gsp == null)
                    return BadRequest();  

                // load parameters
                double ForceRatio(double d) => Math.Max(Math.Min(d, 1.0), 0.0);
                var stockRatio = ForceRatio(gsp.RebalanceParameters.TotalStockRatio);
                var domesticStockRatio = ForceRatio(gsp.RebalanceParameters.DomesticStockRatio);
                var domesticBondRatio = ForceRatio(gsp.RebalanceParameters.DomesticBondRatio);

                // load original portfolio
                var securityCache = new SecurityCache();
                securityCache.Add(gsp.Securities);
                var original = new Portfolio(gsp.Accounts, securityCache);
                
                // rebalance
                var rb = Picker.Rebalance(
                    portfolio: original,
                    stockRatio: stockRatio,
                    domesticStockRatio: domesticStockRatio,
                    domesticBondRatio: domesticBondRatio,
                    debugOutputDirectory: null);
                var report = rb.ToReport();
                return report;
            }
            catch (Exception e)
            {
                // generic catch all
                return BadRequest(e.Message);
            }
        }
    }
}