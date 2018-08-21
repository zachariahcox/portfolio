using System.Collections.Generic;

namespace PortfolioPicker
{
    public abstract class Strategy
    {
        public abstract IReadOnlyList<Order> Perform(IReadOnlyList<Account> accounts);
    }
}
