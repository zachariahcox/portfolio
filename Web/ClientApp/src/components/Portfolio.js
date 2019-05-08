import React, { Component } from 'react';

export class Portfolio extends Component {
    static displayName = Portfolio.name;

    componentDidMount() {
        // call web service with code from editor
        if (this.props.portfolio.data != null) {
            return; // just re-render the cached result
        }

        // fetch new results
        fetch('api/balance', {
            method: 'POST',
            headers: { 'Content-type': 'text/plain' },
            body: this.props.accounts.data
        })
        .then(r => r.json())
        .then(portfolio => {
            this.props.portfolio.save(portfolio);
        })
        .catch(data => {
            console.log(data);
        });
    }

    static formatMoney(amount, decimalCount = 2, decimal = ".", thousands = ",", symbol="$") {
        try {
            decimalCount = Math.abs(decimalCount);
            decimalCount = isNaN(decimalCount) ? 2 : decimalCount;

            const negativeSign = amount < 0 ? "-" : "";

            let i = parseInt(amount = Math.abs(Number(amount) || 0).toFixed(decimalCount)).toString();
            let j = (i.length > 3) ? i.length % 3 : 0;

            return negativeSign + symbol + (j ? i.substr(0, j) + thousands : '')
                + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thousands)
                + (decimalCount ? decimal + Math.abs(amount - i).toFixed(decimalCount).slice(2) : "");

        } catch (e) {
            console.log(e)
        }
    }

    static renderTable(portfolio) {
        const expenseRatio = portfolio.expenseRatio;
        const totalValue = portfolio.totalValue;
        //const positions = portfolio.accounts.map(o => {
        //    var rc = {
        //        id: o.id,
        //        symbol: o.symbol,
        //        value: o.value,
        //        hold: o.hold,
        //    };
        //    return rc;
        //});


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
                        <tr><td>Effective Expense Ratio</td><td>{expenseRatio.toFixed(3)}</td></tr>
                        <tr><td>Total Value</td><td>{Portfolio.formatMoney(totalValue)}</td></tr>
                    </tbody>
                </table>

                <h3>Positions:</h3>
                <table className='table'>
                    <thead>
                        <tr>
                            <th>Account</th>
                            <th>Symbol</th>
                            <th>Value (USD)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {portfolio.accounts.map(a => 
                            a.positions.map(p =>
                                <tr key={p.id}>
                                    <td>{a.name}</td>
                                    <td>{p.symbol}</td>
                                    <td>{Portfolio.formatMoney(p.value)}</td>
                                </tr>
                             ))}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let results = this.props.portfolio.data == null
            ? <p>Please enter account information</p>
            : Portfolio.renderTable(this.props.portfolio.data);

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                {results}
            </div>
        );
    }
}
