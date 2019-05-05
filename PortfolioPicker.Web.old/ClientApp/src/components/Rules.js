import React, { Component } from 'react';

export class Rules extends Component {
    displayName = Rules.name

    render() {
        return (
            <div>
                <h1>Rules behind the four-fund strategy</h1>
                <ol>
                    <li>Accounts prefer funds sponsored by their brokerage</li>
                    <li>Roth accounts should prioritize stocks over bonds</li>
                    <li>Taxable accounts should prioritize international assets over domestic</li>
                    <li>401k accounts should prioritize bonds and avoid international assets</li>
                </ol>

                <p>These rules imply the following asset organization:</p>
                <table className="table">
                    <thead>
                        <tr>
                            <th>Asset Location</th>
                            <th>Asset Type</th>
                            <th>Account Prioritization (highest to lowest)</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Domesetic</td>
                            <td>Stocks</td>
                            <td>
                                <ol>
                                    <li>Roth</li>
                                    <li>Taxable</li>
                                    <li>401k</li>
                                </ol>
                            </td>
                        </tr>
                        <tr>
                            <td>Domesetic</td>
                            <td>Bonds</td>
                            <td>
                                <ol>
                                    <li>401k</li>
                                    <li>Roth</li>
                                    <li>Taxable</li>
                                </ol>
                            </td>
                        </tr>
                        <tr>
                            <td>International</td>
                            <td>Stocks</td>
                            <td>
                                <ol>
                                    <li>Taxable</li>
                                    <li>Roth</li>
                                    <li>401k</li>
                                </ol>
                            </td>
                        </tr>
                        <tr>
                            <td>International</td>
                            <td>Bonds</td>
                            <td>
                                <ol>
                                    <li>Taxable</li>
                                    <li>401k</li>
                                    <li>Roth</li>
                                </ol>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        );
    }
}
