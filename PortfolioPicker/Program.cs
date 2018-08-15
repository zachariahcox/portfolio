using System;
using System.Diagnostics;

namespace PortfolioPicker
{
    class Program
    { 
        static void Main(string[] args)
        {
            //
            // load configurationd file
            //
            if(args.Length < 1)
            {
                Console.WriteLine("ERROR: no data file provided. Please provide a the path to the data json file.");
                Environment.Exit(1);
            }
            Data.Load(args[0]);
#if DEBUG
            Data.Print();
#endif

            //
            // follow a strategy to produce buy orders
            //
            Console.WriteLine("Buy Orders:");
            var orders = Strategy.Perform<FourFund>();
            foreach (var o in orders)
                Console.WriteLine("\t" + o);

// #if DEBUG
//             //
//             // annoying!
//             // block the console from dropping
//             //
//             Console.WriteLine("DEBUG: program comlete: press any key to exit.");
//             Console.Read();
// #endif
        }
    }
}
