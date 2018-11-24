using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioPicker.Strategies;
using Xunit;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        private readonly List<Account> _demo_accounts = new List<Account>
        {
            new Account{
                Name ="roth",
                Brokerage="Vanguard",
                AccountType=AccountType.ROTH,
                Taxable=false,
                Value=100
            },
            new Account{
                Name ="taxable",
                Brokerage="Fidelity",
                AccountType=AccountType.TAXABLE,
                Taxable=true,
                Value=100
            },
            new Account{
                Name ="401k",
                Brokerage="Fidelity",
                AccountType=AccountType.CORPORATE,
                Taxable=false,
                Value=100
            }
        };

        [Fact]
        public void JustVanguard()
        {
            var v = from a in _demo_accounts
                    where a.Brokerage == "Vanguard"
                    select a;

            var accounts = v.ToList();
            var total_value = accounts.Sum(a => a.Value);

            var s = new FourFundStrategy();
            var portfolio = s.Perform(accounts, Data.Funds());
            Assert.Equal(4, portfolio.buy_orders.Count);

            var total_buy_value = portfolio.buy_orders.Sum(o => o.Value);
            Assert.Equal(total_value, total_buy_value);
        }

        [Fact]
        public void JustFidelity()
        {
            var v = from a in _demo_accounts
                    where a.Brokerage == "Fidelity"
                    select a;

            var accounts = v.ToList();
            var total_value = accounts.Sum(a => a.Value);
            var s = new FourFundStrategy();

            Assert.Throws<Exception>(() => s.Perform(accounts, Data.Funds()));
        }
    }
}
