using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    public abstract class Strategy
    {
        public decimal StockRatio { get; set; } = 0.9m;

        public decimal BondsRatio => 1m - StockRatio;

        public decimal StockDomesticRatio { get; set; } = 0.6m;

        public decimal StockInternationalRatio => 1m - StockDomesticRatio;

        public decimal BondsDomesticRatio { get; set; } = 0.7m;

        public decimal BondsInternationalRatio => 1m - BondsDomesticRatio;

        /// <summary>
        /// Available funds
        /// </summary>
        public IReadOnlyList<Fund> Funds 
        { 
            get
            {
                if (_funds is null)
                {
                    _funds = Fund.LoadDefaultFunds();
                }
                return _funds;
            } 
            set
            {
                _funds = value?.OrderBy(x => x.Symbol).ToList();
            }
        }
        [IgnoreDataMember]
        private IReadOnlyList<Fund> _funds;

        /// <summary>
        /// Apply strategy to produce a new portfolio. 
        /// </summary>
        public abstract Portfolio Rebalance(Portfolio p);
    }
}
