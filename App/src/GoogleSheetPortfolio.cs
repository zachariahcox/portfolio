using System.Collections.Generic;
using System.Text.Json;

namespace PortfolioPicker.App
{
    public class GoogleSheetPortfolio 
    {
        public IList<Security> Securities {get;set;}
        public IList<Account> Accounts{get;set;}

        public static GoogleSheetPortfolio FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
                
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
            };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var gsp = JsonSerializer.Deserialize<GoogleSheetPortfolio>(json, options);
            return gsp;
        }
    }
}