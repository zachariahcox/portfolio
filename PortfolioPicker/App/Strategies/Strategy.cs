using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public abstract class Strategy
    {
        public abstract Portfolio Perform(
            IReadOnlyCollection<Account> accounts,
            IDictionary<string, IList<Fund>> funds);
    }
}
