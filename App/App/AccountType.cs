namespace PortfolioPicker.App
{
    public enum AccountType
    {
        None, 
        BROKERAGE, // contributions are after-tax, capital gains on sale.
        IRA, // 401k, contributions are pre-tax, growth is taxable
        ROTH // contributions are after-tax, growth not taxable, includes HSA
    }
}
