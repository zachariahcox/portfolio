using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Fund
    {
        [DataMember(IsRequired =true)]
        public string Symbol { get; set; }

        [DataMember(IsRequired = true)]
        public string Description { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue =false)]
        public string Brokerage { get; set; }

        [DataMember(IsRequired = true)]
        public string Url { get; set; }

        [DataMember(IsRequired = true)]
        public double ExpenseRatio { get; set; } = -1.0;

        [DataMember(IsRequired = false)]
        public double DomesticRatio { get; set; } = 1.0;

        [DataMember(IsRequired = false)]
        public double StockRatio { get; set; } = 1.0;

        [DataMember(IsRequired = false)]
        public bool TargetDate { get; set; } = false;

        [DataMember(IsRequired =false, EmitDefaultValue =false)]
        public string Exposure { get; set; }

        public AssetLocation GetLocation()
        {
            return DomesticRatio == 1.0
                ? AssetLocation.Domestic
                : AssetLocation.International;
        }

        public AssetClass GetClass()
        {
            if (this.TargetDate)
            {
                return AssetClass.TargetDate;
            }
            return StockRatio == 1.0
                ? AssetClass.Stock
                : AssetClass.Bond;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Symbol, ExpenseRatio);
        }
    }
}