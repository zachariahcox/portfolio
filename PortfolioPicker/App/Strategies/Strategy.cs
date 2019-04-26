using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    public abstract class Strategy
    {
        public decimal StockRatio { get; set; } = 0.9m;

        public decimal StockDomesticRatio { get; set; } = 0.6m;

        public decimal BondsDomesticRatio { get; set; } = 0.7m;

        public decimal BondsRatio => 1m - StockRatio;

        public decimal StockInternationalRatio => 1m - StockDomesticRatio;

        public decimal BondsInternationalRatio => 1m - BondsDomesticRatio;

        /// <summary>
        /// Apply strategy to produce a new portfolio. 
        /// </summary>
        public abstract Portfolio Rebalance(Portfolio p);

        public static ICollection<ICollection<T>> Permutations<T>(ICollection<T> list)
        {
            var result = new List<ICollection<T>>();

            // If only one possible permutation, add it and return it
            if (list.Count == 1)
            {
                result.Add(list);
                return result;
            }

            // For each element in that list
            foreach (var element in list)
            {
                var remainingList = new List<T>(list);
                remainingList.Remove(element); // Get a list containing everything except of chosen element

                // Get all possible sub-permutations
                foreach (var permutation in Permutations<T>(remainingList))
                {
                    // Add that element
                    permutation.Add(element);
                    result.Add(permutation);
                }
            }

            return result;
        }
    }
}
