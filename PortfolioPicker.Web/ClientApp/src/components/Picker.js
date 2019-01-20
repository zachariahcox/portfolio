import React, { Component } from 'react';

export class Picker extends Component {
    displayName = Picker.name

    constructor(props) {
        super(props);
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
            
            src: "<enter data or load from file>"
        };

        this.handleUploadData = this.handleUploadData.bind(this);

        this.handleFileChosen = this.handleFileChosen.bind(this);

    }

    handleFileChosen(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.setState({
                src: reader.result
            });
        };
        reader.readAsText(file);
    }

    handleUploadData(ev) {
        // "ev" is a synthetic react event. 
        // prevent the default behavior of the action, whatever it is.
        ev.preventDefault();

        // loading
        this.setState({ loading: true });

        // data payload
        fetch('api/picker', {
            method: 'POST',
            headers: {'Content-type': 'application/json'},
            body: JSON.stringify(JSON.parse(this.finalSrc.textContent))
        })
        .then(r => r.json())
        .then(portfolio => {
            this.setState({
                loading: false,
                hasData: true,

                expenseRatio: portfolio.expenseRatio,
                totalValue: portfolio.totalValue,
                strategy: portfolio.strategy,
                stockPercent: portfolio.stockPercent,
                bondPercent: portfolio.bondPercent,

                buyOrders: portfolio.buyOrders.map(o => {
                    var rc = {
                        id: o.id,
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
                        <tr><td>Effective Expense Ratio</td><td>{portfolio.expenseRatio}</td></tr>
                        <tr><td>Total Value</td><td>${portfolio.totalValue}</td></tr>
                        <tr><td>Stock %</td><td>{portfolio.stockPercent}</td></tr>
                        <tr><td>Bonds %</td> <td>{portfolio.bondPercent}</td></tr>
                    </tbody>
                </table>

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
                        {portfolio.buyOrders.map(o =>
                            <tr key={o.id}>
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
                ? Picker.renderTable(this.state)
                : <p>Please select files.</p>;

        return (
            <div>
                <h1>Suggested Portfolio</h1>
                <p>Upload your data:</p>
                <form onSubmit={this.handleUploadData}>
                    <div>
                        <button>Pick!</button>
                    </div>
                    <br />

                    <input
                        type="file"
                        accept=".json"
                        onChange={e => this.handleFileChosen(e.target.files[0])}
                    />

                    <div
                        contentEditable="true"
                        onChangeText={(newSrc) => this.setState({ src: newSrc })}
                        ref={(ref) => { this.finalSrc = ref; }}
                    >
                    <pre>{this.state.src}</pre>
                    </div>

                    <br />
                </form>
                {results}
            </div>
        );
    }
}
