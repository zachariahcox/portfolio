import React, { Component } from 'react';
import { Container, Row, Col } from 'reactstrap';
import { Editor } from './Editor';

export class Controls extends Component {
    static displayName = Controls.name;
    render() {
        return (
            <div>
                <h1>Controls</h1>
                <Container>
                    <Row>
                        <Col>What percent of total assets should be in stocks?</Col>
                        <Col>
                            <input
                                type="number"
                                defaultValue={this.props.totalStock.data} />
                        </Col>
                    </Row>
                    <Row>
                        <Col>What percent of stocks should be domestic?</Col>
                        <Col>
                            <input
                                type="number"
                                defaultValue={this.props.domesticStock.data} />
                        </Col>
                    </Row>
                    <Row>
                        <Col>What percent of bonds should be domestic?</Col>
                        <Col>
                            <input
                                type="number"
                                defaultValue={this.props.domesticBonds.data} />
                        </Col>
                    </Row>
                    <Row>
                        <Col>
                            <Editor dataReference={this.props.accounts} />
                        </Col>
                    </Row>
                    <Row>
                        <Col>
                            <Editor dataReference={this.props.funds} />
                        </Col>
                    </Row>
                </Container>
            </div>
        );
    }
}
