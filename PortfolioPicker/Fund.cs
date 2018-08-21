using System;
using System.Collections.Generic;

namespace PortfolioPicker
{
    public class Fund
    {
        public String symbol;
        public String description;
        public String url; 
        public double expense_ratio = -1.0;
        public bool domestic = true;
        public bool stock = true;
        public String exposure;



        public override string ToString()
        {
            return String.Format("{0} ({1})", symbol, expense_ratio);
            //String.Join("\n\t", new List<String> {
            //    symbol,
            //    description,
            //    "Expense Ratio: " + expense_ratio.ToString(),
            //    "Prospectus: " + url});
        }
    }
}