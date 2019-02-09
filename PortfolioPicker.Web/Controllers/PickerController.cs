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
        [HttpPost]
        public Portfolio Post()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var yaml = reader.ReadToEnd();
                var p = Picker.Create(yaml);
                return p.Pick();
            }
        }
    }
}