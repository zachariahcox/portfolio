using System.Collections.Generic;

namespace PortfolioPicker
{
    public abstract class Strategy
    {
        public abstract Portfolio Perform(IReadOnlyList<Account> accounts);
    }
}
