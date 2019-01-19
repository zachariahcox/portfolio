import React, { Component } from 'react';

export class Picker extends Component {
    displayName = Picker.name

    constructor(props) {
        super(props);
        this.state = {
            expenseRatio: 0,
            buyOrders: [],
            loading: false,
            hasData: false
        };

        this.handleUploadData = this.handleUploadData.bind(this);
    }

    handleUploadData(ev) {
        // "ev" is a synthetic react event. 
        // prevent the default behavior of the action, whatever it is.
        ev.preventDefault();

        // loading
        this.setState({ loading: true });

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
                            fund: o.fund,
                            account: o.account.name,
                            value: o.value,
                            symbol: "",
                            url: "",
                            description: "",
                            domestic: null,
                            stock: null
                        };

                        const f = o.fund;
                        if (f) {
                            rc.symbol = f.symbol;
                            rc.url = f.url;
                            rc.description = f.description;
                            rc.domestic = f.domestic;
                            rc.stock = f.stock;
                        }
                        return rc;
                    }),

                    loading: false,
                    hasData: true
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
                            <th>Symbol</th>
                            <th>Class</th>
                            <th>Account</th>
                            <th>Value (USD)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {buyOrders.map(o =>
                            <tr key={o.account}>
                                <td><a href={o.url}>{o.symbol}</a></td>
                                <td>{o.stock ? "Stock" : "Bond"}</td>
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
        // Message based on state
        let results = this.state.loading
            ? <p><em>loading...</em></p>
            : this.state.hasData
                ? Picker.renderTable(this.state.expenseRatio, this.state.buyOrders)
                : <p>Please select files.</p>;

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                <p>Upload your data:</p>
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
