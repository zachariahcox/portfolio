import React, { Component } from 'react';

export class Picker extends Component {
    displayName = Picker.name

    constructor(props) {
        super(props);
        this.state = {
            expenseRatio: 0,
            buyOrders: [],
            loading: true
        };

        // fetch sample portfolio
        fetch('api/Picker')
            .then(response => response.json())
            .then(result => {
                this.setState({
                    expenseRatio: result.expenseRatio,
                    buyOrders: result.buyOrders.map(o => 
                    {
                        var rc = new Object();
                        rc.fund = o.fund.symbol;
                        rc.account = o.account.name;
                        rc.value = o.value;
                        return rc;
                    }),
                    loading: false
                });
            });
    }

    static renderTable(er, buyOrders) {
        return (
            <div>
                <h2>Total Expense Ratio: {er}</h2>
                <h3>Buy Orders:</h3>
                <table className='table'>
                    <thead>
                        <tr>
                            <th>Fund</th>
                            <th>Account</th>
                            <th>Value (USD)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {buyOrders.map(o => 
                            <tr key={o.value}>
                                <td>{o.fund}</td>
                                <td>{o.account}</td>
                                <td>{o.value}</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Picker.renderTable(this.state.expenseRatio, this.state.buyOrders);

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                <p>A buggy robot did its best:</p>
                {contents}
            </div>
        );
    }
}
