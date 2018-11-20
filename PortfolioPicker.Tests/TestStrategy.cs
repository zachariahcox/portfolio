using System.Collections.Generic;
using PortfolioPicker.Strategies;
using Xunit;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        private readonly IReadOnlyList<Account> _demo_accounts;
        public TestStrategy()
        {
            _demo_accounts = new List<Account>
            {
                new Account{
                    name ="roth",
                    brokerage="Vanguard",
                    type=AccountType.ROTH,
                    taxable=false,
                    value=100000
                },
                new Account{
                    name ="taxable",
                    brokerage="Fidelity",
                    type=AccountType.INVESTMENT,
                    taxable=true,
                    value=100000
                },
                new Account{
                    name ="401k",
                    brokerage="Fidelity",
                    type=AccountType.CORPORATE,
                    taxable=false,
                    value=100000
                }
            };
        }

        [Fact]
        public void BuysFourFunds()
        {
            var s = new FourFundStrategy();
            var portfolio = s.Perform(_demo_accounts);
            Assert.Equal(4, portfolio.buy_orders.Count);
        }
    }
}
