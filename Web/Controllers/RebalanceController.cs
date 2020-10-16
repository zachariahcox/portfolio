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
                var stockRatio = 0.9;
                var domesticStockRatio = 0.6;
                var domesticBondRatio = 1.0;
                gsp.RebalanceParameters.TryGetValue(GoogleSheetPortfolio.TotalStockRatio, out stockRatio);
                gsp.RebalanceParameters.TryGetValue(GoogleSheetPortfolio.DomesticStockRatio, out domesticStockRatio);
                gsp.RebalanceParameters.TryGetValue(GoogleSheetPortfolio.DomesticBondRatio, out domesticBondRatio);
                stockRatio = Math.Max(Math.Min(stockRatio, 1.0), 0.0);
                domesticStockRatio = Math.Max(Math.Min(domesticStockRatio, 1.0), 0.0);
                domesticBondRatio = Math.Max(Math.Min(domesticBondRatio, 1.0), 0.0);

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