using System.Collections.Generic;

namespace PortfolioPicker
{
    public abstract class Strategy
    {
        public abstract Portfolio Perform(
            IReadOnlyCollection<Account> accounts,
            FundsByBrokerageMap funds);
    }
}
