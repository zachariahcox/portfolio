using Microsoft.AspNetCore.Mvc;
using PortfolioPicker.App;

namespace PortfolioPicker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PickerController : ControllerBase
    {
        [HttpPost]
        public void Post([FromBody] string accounts)
        {
            var p = new Picker(accounts, "FourFundStrategy");

        }
    }
}
