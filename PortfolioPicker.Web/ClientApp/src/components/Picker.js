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
        this.setState({
            loading: true,
            src: ''
        });

        // data payload
        fetch('api/picker', {
            method: 'POST',
            headers: { 'Content-type': 'text/plain'},
            body: this.finalSrc.textContent
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
                        stock: 1,
                        domestic: 1
                    };

                    const f = o.fund;
                    if (f) {
                        rc.symbol = f.symbol;
                        rc.url = f.url;
                        rc.description = f.description;
                        rc.domestic = f.domesticRatio;
                        rc.stock = f.stockRatio;
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
                                <td>{o.stock}</td>
                                <td>{1.0 - o.stock}</td>
                                <td>{o.domestic}</td>
                                <td>{1.0 - o.domestic}</td>
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
                        <button>Do it!</button>
                    </div>
                    <br />

                    <input
                        type="file"
                        accept=".yaml"
                        onChange={e => this.handleFileChosen(e.target.files[0])}
                    />

                    <div ref={(ref) => { this.finalSrc = ref; }}>
                        <pre>{this.state.src}</pre>
                    </div>
                    <br />
                </form>
                {results}
            </div>
        );
    }
}
