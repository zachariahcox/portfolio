namespace PortfolioPicker.App
{
    public class ScoreWeights
    {
        public double AssetMix {get;set;}
        public double TaxEfficiency {get;set;}
        public double ExpenseRatio {get;set;}
        public double TaxableSales {get;set;}

        // tax efficiency weights
        public double StockDomesticBrokerage {get;set;}
        public double StockDomesticIra {get;set;}
        public double StockDomesticRoth {get;set;}

        public double StockInternationalBrokerage {get;set;}
        public double StockInternationalIra {get;set;}
        public double StockInternationalRoth {get;set;}

        public double BondDomesticBrokerage {get;set;}
        public double BondDomesticIra {get;set;}
        public double BondDomesticRoth {get;set;}

        public double BondInternationalBrokerage {get;set;}
        public double BondInternationalIra {get;set;}
        public double BondInternationalRoth {get;set;}
        
        public bool IsValid()
        {
            bool Fine(double d) => d >= 0 && d <= 1; // between 0 and 1
            return 
                100.0 == AssetMix + TaxEfficiency + ExpenseRatio // total weights should sum to 100
                && Fine(StockDomesticBrokerage)
                && Fine(StockDomesticIra)
                && Fine(StockDomesticRoth)
                && Fine(StockInternationalBrokerage)
                && Fine(StockInternationalIra)
                && Fine(StockInternationalRoth)
                && Fine(BondDomesticBrokerage)
                && Fine(BondDomesticIra)
                && Fine(BondDomesticRoth)
                && Fine(BondInternationalBrokerage)
                && Fine(BondInternationalIra)
                && Fine(BondInternationalRoth)
                ;
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
        public double GetWeight(AssetClass c, AssetLocation l, AccountType t)
        {
            if (c == AssetClass.Stock && l == AssetLocation.Domestic && t == AccountType.BROKERAGE) return StockDomesticBrokerage;
            if (c == AssetClass.Stock && l == AssetLocation.Domestic && t == AccountType.IRA) return StockDomesticIra;
            if (c == AssetClass.Stock && l == AssetLocation.Domestic && t == AccountType.ROTH) return StockDomesticRoth;

            if (c == AssetClass.Stock && l == AssetLocation.International && t == AccountType.BROKERAGE) return StockInternationalBrokerage;
            if (c == AssetClass.Stock && l == AssetLocation.International && t == AccountType.IRA) return StockInternationalIra;
            if (c == AssetClass.Stock && l == AssetLocation.International && t == AccountType.ROTH) return StockInternationalRoth;

            if (c == AssetClass.Bond && l == AssetLocation.Domestic && t == AccountType.BROKERAGE) return BondDomesticBrokerage;
            if (c == AssetClass.Bond && l == AssetLocation.Domestic && t == AccountType.IRA) return BondDomesticIra;
            if (c == AssetClass.Bond && l == AssetLocation.Domestic && t == AccountType.ROTH) return BondDomesticRoth;

            if (c == AssetClass.Bond && l == AssetLocation.International && t == AccountType.BROKERAGE) return BondInternationalBrokerage;
            if (c == AssetClass.Bond && l == AssetLocation.International && t == AccountType.IRA) return BondInternationalIra;
            if (c == AssetClass.Bond && l == AssetLocation.International && t == AccountType.ROTH) return BondInternationalRoth;
            
            return 0;
        }

        public static ScoreWeights Default()
        {
            return new ScoreWeights()
            {
                // these weights are kind of arbitrary but need to sum to 100. 
                AssetMix      = 40, 
                TaxEfficiency = 30, 
                ExpenseRatio  = 30,
                
                // how much to penalize taxable sales, as a multiplier on ratio 
                TaxableSales  = 1, 

                StockDomesticBrokerage = 0, // high growth? put in tax-advantaged accounts? 
                StockDomesticIra       = 1, // fine
                StockDomesticRoth      = 1, // fine

                StockInternationalBrokerage = 1, // foreign income tax credit
                StockInternationalIra       = 0, // fine? 
                StockInternationalRoth      = 0, // fine? 

                BondDomesticBrokerage = 1, // you have to put something in the brokerage accounts
                BondDomesticIra       = 0.25, // not ideal? 
                BondDomesticRoth      = 0, // the worst

                BondInternationalBrokerage = 1, // low growth + foreign income tax credit
                BondInternationalIra       = 0, // fine? 
                BondInternationalRoth      = 0, // fine? 
            };
        }
    }
}
