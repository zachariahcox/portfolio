﻿import React, { Component } from 'react';

export class Picker extends Component {
    displayName = Picker.name

    constructor(props) {
        super(props);
        this.state = {
            expenseRatio: 0,
            buyOrders: [],
            loading: true
        };

        this.handleUploadData = this.handleUploadData.bind(this);
    }

    handleUploadData(ev) {

        ev.preventDefault();

        // data payload
        const data = new FormData();
        data.append('file', this.uploadInput.files[0]);
        data.append('filename', 'data.json');
        fetch('api/picker', { method: 'POST', body: data })
            .then(response => response.json())
            .then(portfolio => {
                this.setState({
                    expenseRatio: portfolio.expenseRatio,
                    buyOrders: portfolio.buyOrders.map(o => {
                        var rc = {
                            fund: (o.fund) ? o.fund.symbol : "unallocated",
                            account: o.account.name,
                            value: o.value
                        };
                        return rc;
                    }),
                    loading: false
                });
            });
    }

    static renderTable(expenseRatio, buyOrders) {
        return (
            <div>
                <h2>Total Expense Ratio: {expenseRatio}</h2>
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
        let results = this.state.loading
            ? <p><em>Loading...</em></p>
            : Picker.renderTable(this.state.expenseRatio, this.state.buyOrders);

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                <p>Upload your data</p>
                <form onSubmit={this.handleUploadData}>
                    <div>
                        <input
                            ref={(ref) => { this.uploadInput = ref; }}
                            type="file" />
                    </div>
                    <br />
                    <div>
                        <button>Upload</button>
                    </div>
                </form>
                {results}
            </div>
        );
    }
}
