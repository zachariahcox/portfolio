using System;
using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class StrategyException: Exception
    {
        public IReadOnlyList<string> Errors { get; set; }
        public StrategyException(IReadOnlyList<string> errors)
        {
            this.Errors = errors;
        }
    }
}
