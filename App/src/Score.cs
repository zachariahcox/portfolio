namespace PortfolioPicker.App
{
    public class Score
    {   
        private double _Total = -1;
        public double Total => _Total < 0 
        ? _Total = (
            AssetMixWeight * AssetMix
            + TaxEfficiencyWeight * TaxEfficiency
            + ExpenseRatioWeight * ExpenseRatio
            ) / 100.0
        : _Total;

        private double _TotalIncludingSales = -1;
        public double RebalanceTotal => _TotalIncludingSales < 0
        ? _TotalIncludingSales = Total - TaxableSalesWeight / 100.0 * TaxableSales
        : _TotalIncludingSales;

        public double AssetMix;
        public double AssetMixWeight;
        public double TaxEfficiency;
        public double TaxEfficiencyWeight;
        public double ExpenseRatio;
        public double ExpenseRatioWeight;
        public double TaxableSales;
        public double TaxableSalesWeight;
        
        // weight by number of orders? 
        // public double OrdersScore;
        // public double OrdersScoreWeight;

        public Score DeepClone()
        {
            return new Score(){
                _Total = this._Total,
                _TotalIncludingSales = this._TotalIncludingSales,
                AssetMix = this.AssetMix,
                AssetMixWeight = this.AssetMixWeight,
                TaxEfficiency = this.TaxEfficiency,
                TaxEfficiencyWeight = this.TaxEfficiencyWeight,
                ExpenseRatio = this.ExpenseRatio, 
                ExpenseRatioWeight = this.ExpenseRatioWeight,
                TaxableSales = this.TaxableSales,
                TaxableSalesWeight = this.TaxableSalesWeight
            };
        }

        /// <summary>
        /// Based on the target exposures and total money, compute target dollar-value per exposure type.
        ///  Strategy: 
        ///  * accounts prefer funds sponsored by their brokerage
        ///    * Helps avoid fees?
        ///
        ///  * roth accounts should prioritize stocks over bonds
        ///    * growth can be withdrawn tax-free, prioritize high-growth-potential products.
        ///
        ///  * regular brokerage accounts should prioritize international assets over domestic
        ///    * foreign income tax credit is deductible
        /// 
        ///  * 401k accounts should prioritize bonds and avoid international assets
        ///    * because growth is taxable, prioritize low-growth products
        /// 
        ///  * tax-advantaged accounts should be generally preferred over brokerage accounts
        //
        /// </summary>
        public static double GetScoreWeight(AssetClass c, AssetLocation l, AccountType t)
        {
            if (c == AssetClass.Stock && l == AssetLocation.Domestic)
            {
                // stock, domestic
                switch (t)
                {
                    case AccountType.BROKERAGE: return 0; // really do not want this
                    case AccountType.IRA:       return 1; // fine
                    case AccountType.ROTH:      return 1; // fine
                    default: return 0;
                }
            }
            else if(c == AssetClass.Stock && l == AssetLocation.International)
            {     
                switch (t)
                {
                    case AccountType.BROKERAGE: return 1; // really want this
                    case AccountType.IRA:       return 0; // neither of these are great
                    case AccountType.ROTH:      return 0;
                    default: return 0;
                }
            }
            else if (c == AssetClass.Bond && l == AssetLocation.Domestic)
            {
                // bond, domestic
                switch (t)
                {
                    case AccountType.BROKERAGE: return 1; // you have to put something in the brokerage accounts
                    case AccountType.IRA:       return .25; // not ideal
                    case AccountType.ROTH:      return 0; // the worst
                    // default: return 0;
                }
            }
            else if (c == AssetClass.Bond && l == AssetLocation.International)
            {
                // bond, international
                switch (t){
                    case AccountType.BROKERAGE: return 1; // low growth + foreign income tax credit
                    case AccountType.IRA:       return 0; // neither of these are great
                    case AccountType.ROTH:      return 0;
                    default: return 0;
                }
            }

            // anything else gets no points
            return 0;
        }
    
        public const double weight_assetMix = 40;
        public const double weight_useTaxOptimalAccounts = 30;
        public const double weight_lowExpenseRatio = 30;
        public const double weight_taxableSales = 1;
    }
}