import React, { Component } from 'react';

export class Portfolio extends Component {
    displayName = Portfolio.name

    constructor(props) {
        super(props);
        this.code = props.code;

        this.state = {
            loading: false,
            hasData: false,

            // portfolio values
            expenseRatio: 0,
            totalValue: 0,
            strategy: "",
            stockPercent: 0,
            bondPercent: 0,
            buyOrders: [],
        };
    }

    componentDidMount() {
        // call web service with code from editor
        // loading
        this.setState({
            loading: true,
        });

        // data payload
        fetch('api/picker', {
            method: 'POST',
            headers: { 'Content-type': 'text/plain' },
            body: this.props.code
        })
            .then(r => r.json())
            .then(portfolio => {
                this.setState({
                    loading: false,
                    hasData: true,

                    expenseRatio: portfolio.expenseRatio,
                    totalValue: portfolio.totalValue,
                    strategy: portfolio.strategy,
                    stockPercent: 100.0 * portfolio.stockRatio,
                    bondPercent: 100.0 * portfolio.bondRatio,

                    buyOrders: portfolio.buyOrders.map(o => {
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
                    })
                });
            })
            .catch(data => {
                console.log(data);
            });
    }

    static renderTable(portfolio) {
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
                        <tr><td>Strategy</td><td>{portfolio.strategy}</td></tr>
                        <tr><td>Effective Expense Ratio</td><td>{portfolio.expenseRatio.toFixed(2)}</td></tr>
                        <tr><td>Total Value</td><td>${portfolio.totalValue}</td></tr>
                        <tr><td>Stock %</td><td>{portfolio.stockPercent.toFixed(2)}</td></tr>
                        <tr><td>Bonds %</td> <td>{portfolio.bondPercent.toFixed(2)}</td></tr>
                    </tbody>
                </table>

                <h3>Buy Orders:</h3>
                <table className='table'>
                    <thead>
                        <tr>
                            <th>Account</th>
                            <th>Symbol</th>
                            <th>Stock%</th>
                            <th>Bond%</th>
                            <th>Domestic%</th>
                            <th>International%</th>
                            <th>Value (USD)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {portfolio.buyOrders.map(o =>
                            <tr key={o.id}>
                                <td>{o.account}</td>
                                <td><a href={o.url}>{o.symbol}</a></td>
                                <td align="right">{o.stock.toFixed(2)}</td>
                                <td align="right">{(100.0 - o.stock).toFixed(2)}</td>
                                <td align="right">{o.domestic.toFixed(2)}</td>
                                <td align="right">{(100.0 - o.domestic).toFixed(2)}</td>
                                <td align="right">${o.value.toFixed(2)}</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let results = this.state.hasData
            ? Portfolio.renderTable(this.state)
            : <p>Please enter account information</p>;

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                {results}
            </div>
        );
    }
}
