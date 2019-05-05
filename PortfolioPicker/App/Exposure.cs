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

        public AssetClass Class { get; set; }

        public AssetLocation Location { get; set; }

        static ExposureAccountPreference[] _preferences;
        public static AccountType[] AssetPreference(
            AssetClass c,
            AssetLocation l)
        {
            if (_preferences == null)
            {
                _preferences = new[]
                {
                    new ExposureAccountPreference (
                        AssetClass.Stock,
                        AssetLocation.Domestic,
                        new AccountType[] {
                            AccountType.ROTH,
                            AccountType.BROKERAGE,
                            AccountType.IRA
                        }
                    ),

                    new ExposureAccountPreference (
                        AssetClass.Stock,
                        AssetLocation.International,
                        new AccountType[] {
                            AccountType.BROKERAGE,
                            AccountType.ROTH,
                            AccountType.IRA
                        }
                        ),

                    new ExposureAccountPreference (
                        AssetClass.Bond,
                        AssetLocation.Domestic,
                        new AccountType[] {
                            AccountType.IRA,
                            AccountType.BROKERAGE,
                            AccountType.ROTH,
                        }),

                    new ExposureAccountPreference (
                        AssetClass.Bond,
                        AssetLocation.International,
                        new AccountType[] {
                            AccountType.BROKERAGE,
                            AccountType.IRA,
                            AccountType.ROTH
                        }),
                };
            }

            return _preferences
                .FirstOrDefault(x => c == x.Class && l == x.Location)
                ?.Types;
        }
    }

    public class ExposureTarget : Exposure
    {
        public ExposureTarget(AssetClass c, AssetLocation l, decimal target)
            : base(c, l)
        {
            Target = target;
        }
        public decimal Target { get; set; }
    }

    public class ExposureAccountPreference : Exposure
    {
        public ExposureAccountPreference(
            AssetClass c, 
            AssetLocation l, 
            AccountType[] types)
            : base(c, l)
        {
            Types = types;
        }

        public AccountType[] Types { get; set; }
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
                r = 0.0;

            Ratio = Math.Max(0, r);
        }

        public double Ratio { get; set; }
    }

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
