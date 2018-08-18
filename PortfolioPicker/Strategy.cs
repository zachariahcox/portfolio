using System.Collections.Generic;

namespace PortfolioPicker
{
    public abstract class Strategy
    {
        //public abstract IReadOnlyList<Order> Perform();
        public abstract IReadOnlyList<Order> Perform(Data data);
        
        //public static IReadOnlyList<Order> Perform<T>() 
        //    where T : Strategy, new()
        //{
        //    return new T().Perform();
        //}
    }
}
