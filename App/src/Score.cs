namespace PortfolioPicker.App
{
    public class Score
    {   
        private double _Total = -1;
        public double Total => _Total < 0 
        ? _Total = (
            this.Weights.AssetMix * AssetMix
            + this.Weights.TaxEfficiency * TaxEfficiency
            + this.Weights.ExpenseRatio * ExpenseRatio
            ) / 100.0
        : _Total;

        private double _TotalIncludingSales = -1;
        public double RebalanceTotal => _TotalIncludingSales < 0
        ? _TotalIncludingSales = Total - this.Weights.TaxableSales / 100.0 * TaxableSales
        : _TotalIncludingSales;

        public double AssetMix;
        public double TaxEfficiency;
        public double ExpenseRatio;
        public double TaxableSales;
        public ScoreWeights Weights = new ScoreWeights();

        // weight by number of orders? 
        // public double OrdersScore;
        // public double OrdersScoreWeight;
    }
}