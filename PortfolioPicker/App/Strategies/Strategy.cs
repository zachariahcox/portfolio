using System.Collections.Generic;

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

        public abstract Portfolio Perform(
            IReadOnlyCollection<Account> accounts,
            IReadOnlyList<Fund> funds);
    }
}
