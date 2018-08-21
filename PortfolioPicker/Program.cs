using System;

namespace PortfolioPicker
{
    class Program
    { 
        static void Main(string[] args)
        {
            //
            // load configuration file
            //
            if(args.Length < 1)
            {
                Console.WriteLine("ERROR: no data file provided. Please provide a the path to the data json file.");
                Environment.Exit(1);
            }
            var data = Data.Load(args[0]);
#if DEBUG
            data.Print();
#endif

            //
            // Load strategy
            //
            var strategy_name = args.Length < 2 ? "FourFundStrategy" : args[1];
            var strategy_type = Type.GetType("PortfolioPicker." + strategy_name);
            var strategy = (Strategy)Activator.CreateInstance(strategy_type);

            //
            // follow a strategy to produce buy orders
            //
            var orders = strategy.Perform(data.accounts);
            Console.WriteLine("Buy Orders:");
            foreach (var o in orders)
                Console.WriteLine("\t" + o);
        }
    }
}
