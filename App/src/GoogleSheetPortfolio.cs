using System.Collections.Generic;
using System.Text.Json;

namespace PortfolioPicker.App
{
    public class RebalanceParameters
    {
        public double TotalStockRatio {get;set;} = 0.9;
        public double DomesticStockRatio {get;set;} = 0.6;
        public double DomesticBondRatio {get;set;} = 1.0;
        public string Url {get;set;}
    }

    public class GoogleSheetPortfolio 
    {
        public IList<Security> Securities {get;set;}
        public IList<Account> Accounts{get;set;}
        public RebalanceParameters RebalanceParameters {get; set;}

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