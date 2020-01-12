using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    /// <summary>
    /// Based on the target exposures and total money, compute target dollar-value per exposure type.
    ///  Strategy: 
    ///  * accounts prefer funds sponsored by their brokerage
    ///    * Helps avoid fees?
    ///
    ///  * roth accounts should prioritize stocks over bonds
    ///    * Growth is not taxable, prioritize high-growth potential products.
    ///
    ///  * taxable accounts should prioritize international assets over domestic
    ///    * foreign income tax credit
    /// 
    ///  * 401k accounts should prioritize bonds and avoid international assets
    ///    Because growth is taxable, prioritize low-growth products
    ///
    /// This basically works out to the following exposure-to-account type prioritization list:
    ///  dom stocks -> roth, tax, 401k
    ///  int stocks -> tax, roth, 401k
    ///  dom bonds  -> 401k, roth, tax
    ///  int bonds  -> tax, 401k, roth
    /// </summary>
    public class Exposure
    {
        public Exposure(AssetClass c, AssetLocation l)
        {
            Class = c;
            Location = l;
        }

        public AssetClass Class { get; private set; }

        public AssetLocation Location { get; private set; }
    }

    /// <summary>
    /// target value allocated to a given class / location combo
    /// </summary>
    public class ExposureTarget : Exposure
    {
        public ExposureTarget(
            AssetClass c, 
            AssetLocation l, 
            decimal target) 
            : base(c, l)
        {
            // save target
            Target = target;

            // generate type preferences
            if (c == AssetClass.Stock)
            {
                if (l == AssetLocation.Domestic)
                {
                    // stock, domestic
                    Types = new AccountType[] {
                        AccountType.ROTH,
                        AccountType.BROKERAGE,
                        AccountType.IRA
                    };
                }
                else 
                {
                    // stock, international
                    Types = new AccountType[] {
                        AccountType.BROKERAGE,
                        AccountType.ROTH,
                        AccountType.IRA
                    };
                }
            }
            else 
            {
                if (l == AssetLocation.Domestic)
                {
                    // bond, domestic
                    Types = new AccountType[] {
                        AccountType.IRA,
                        AccountType.BROKERAGE,
                        AccountType.ROTH,
                    };
                }
                else 
                {
                    // bond, international
                    Types = new AccountType[] {
                        AccountType.BROKERAGE,
                        AccountType.IRA,
                        AccountType.ROTH
                    };
                }
            }
        }

        /// <summary> 
        /// final value goal
        /// </summary>
        public decimal Target { get; private set; }

        /// <summary>
        /// ordered type preferences for this combination of class and location
        /// </summary>
        public AccountType[] Types { get; private set; }
    }

    public class ExposureRatio : Exposure
    {
        public ExposureRatio(
            AssetClass c,
            AssetLocation l,
            double r)
            : base(c, l)
        {
            if (double.IsNaN(r))
            {
                r = 0.0;
            }

            Ratio = Math.Max(0, r);
        }

        public double Ratio { get; set; }
    }

    /// <summary>
    /// Add some functionality to lists of ExposureRatio
    /// </summary>
    public static class ExposureExtensions
    {
        public static double Percent(
            this IList<ExposureRatio> ratios,
            AssetClass c)
        {
            return 100.0 * ratios.Where(x => x.Class == c).Sum(x => x.Ratio);
        }

        public static double Percent(
            this IList<ExposureRatio> ratios,
            AssetLocation l)
        {
            return 100.0 * ratios.Where(x => x.Location == l).Sum(x => x.Ratio);
        }

        public static double Percent(
            this IList<ExposureRatio> ratios,
            AssetClass c,
            AssetLocation l)
        {
            return 100.0 * ratios.Where(x => x.Class == c && x.Location == l).Sum(x => x.Ratio);
        }
    }
}
