using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Position
    {
        [DataMember(IsRequired=true)]
        public string Symbol { 
            get => _symbol; 
            set => _symbol= value is null ? null : value.ToLower().Trim(); 
            }

        [DataMember(IsRequired=true)]
        public double Value { get; set; }

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public double Quantity {get; set;}

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public bool Hold { get; set; }

        public override string ToString() => $"{Symbol}@{Value}";

        [IgnoreDataMember]
        private string _symbol;
    }
}
