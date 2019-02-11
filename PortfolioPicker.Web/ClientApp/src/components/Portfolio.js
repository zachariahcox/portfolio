import React, { Component } from 'react';

export class Portfolio extends Component {
    displayName = Portfolio.name

    componentDidMount() {
        // call web service with code from editor

        if (this.props.portfolio != null) {
            return; // just rerender the cached result
        }
   
        // fetch new results
        fetch('api/picker', {
            method: 'POST',
            headers: { 'Content-type': 'text/plain' },
            body: this.props.code
        })
        .then(r => r.json())
        .then(portfolio => {
            this.props.cachePortfolio(portfolio);
        })
        .catch(data => {
            console.log(data);
        });
    }

    static renderTable(portfolio) {
        const expenseRatio = portfolio.expenseRatio;
        const totalValue = portfolio.totalValue;
        const strategy = portfolio.strategy;
        const stockPercent = 100.0 * portfolio.stockRatio;
        const bondPercent = 100.0 * portfolio.bondRatio;
        const buyOrders = portfolio.buyOrders.map(o => {
            var rc = {
                id: o.id,
                fund: o.fund,
                account: o.account.name,
                value: o.value,
                symbol: "",
                url: "",
                description: "",
                stock: 100,
                domestic: 100
            };

            const f = o.fund;
            if (f) {
                rc.symbol = f.symbol;
                rc.url = f.url;
                rc.description = f.description;
                rc.domestic = 100.0 * f.domesticRatio;
                rc.stock = 100.0 * f.stockRatio;
            }
            return rc;
        });


        return (
            <div>
                <h3>Summary:</h3>
                <table className='table'>
                    <thead>
                        <tr>
                            <th>Statistic</th>
                            <th>Value</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td>Strategy</td><td>{strategy}</td></tr>
                        <tr><td>Effective Expense Ratio</td><td>{expenseRatio.toFixed(2)}</td></tr>
                        <tr><td>Total Value</td><td>${totalValue.toFixed(2)}</td></tr>
                        <tr><td>Stock</td><td>%{stockPercent.toFixed(2)}</td></tr>
                        <tr><td>Bonds</td> <td>%{bondPercent.toFixed(2)}</td></tr>
                    </tbody>
                </table>

                <h3>Buy Orders:</h3>
                <table className='table'>
                    <thead>
                        <tr>
                            <th>Account</th>
                            <th>Symbol</th>
                            <th>Stock</th>
                            <th>Bond</th>
                            <th>Domestic</th>
                            <th>International</th>
                            <th>Value (USD)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {buyOrders.map(o =>
                            <tr key={o.id}>
                                <td>{o.account}</td>
                                <td><a href={o.url}>{o.symbol}</a></td>
                                <td>%{o.stock.toFixed(2)}</td>
                                <td>%{(100.0 - o.stock).toFixed(2)}</td>
                                <td>%{o.domestic.toFixed(2)}</td>
                                <td>%{(100.0 - o.domestic).toFixed(2)}</td>
                                <td>${o.value.toFixed(2)}</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let results = this.props.portfolio == null
            ? <p>Please enter account information</p>
            : Portfolio.renderTable(this.props.portfolio);

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                {results}
            </div>
        );
    }
}
