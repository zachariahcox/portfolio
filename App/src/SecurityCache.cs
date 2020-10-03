using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace PortfolioPicker.App
{
    public class SecurityCache
    {
        public ConcurrentBag<Security> Cache {get;} = new ConcurrentBag<Security>();
        
        public ConcurrentDictionary<string, IList<Security>> CacheByBrokerage { get;} = new ConcurrentDictionary<string, IList<Security>>();

        public Security Get(string symbol)
        {
            var result = Cache.FirstOrDefault(x => x.Symbol == symbol || x.SymbolMap == symbol);
            if (result == null)
            {
                // found new product apparently?
                if (symbol == Cash.CASH)
                    result = new Cash();
                else 
                {
                    // TODO load stats from service? 
                    result = new Security
                    {
                        Symbol = symbol,
                        StockRatio = 1,
                        DomesticRatio = 1,
                        ExpenseRatio = 0,
                        Description = "domestic stock?"
                    };
                }
                Cache.Add(result);
            }
            return result;
        }

        /// <summary>
        /// list of securities specifically allowed for this security group
        /// </summary>
        public IEnumerable<Security> GetSecurityGroup(string brokerage)
        {
            // use cache
            if (!CacheByBrokerage.TryGetValue(brokerage, out var securitiesInGroup))
            {
                securitiesInGroup = Cache?
                    .Where(x => x.Symbol == Cash.CASH || x.Brokerage == brokerage)
                    .OrderBy(x => x.Symbol)
                    .ToArray();

                CacheByBrokerage[brokerage] = securitiesInGroup;
            }
            return securitiesInGroup;
        }

        public void Add(IEnumerable<Security> securities)
        {
            if (securities == null)
                return;

            foreach (var s in securities)
            {
                if (Cache.Contains(s))
                    return;
                Cache.Add(s);
            }
        }
    }
}