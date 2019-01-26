import React, { Component } from 'react';
import MonacoEditor from 'react-monaco-editor';
//import { Col, Grid, Row } from 'react-bootstrap';
//class EditorLayout extends Component {
//    displayName = EditorLayout.name
//    render() {
//        return (
//            <Grid fluid>
//                <Row sm={2}>
//                    <Col>
//                        {this.props.children}
//                    </Col>
//                </Row>
//                <Row sm={10}>
//                    <Col>
//                        {this.props.children}
//                    </Col>
//                </Row>
//            </Grid>
//        );
//    }
//}

export class Editor extends Component {
    displayName = Editor.name
    constructor(props) {
        super(props);
        this.updateCode = props.updateCode;
        this.onChange = (newValue, event) => {
            //this.updateCode(newValue);
        };
    }

    render() {
        const code = this.props.code;
        const change = this.onChange;
        const options = {
            selectOnLineNumbers: true
        };
        return (
            <div>
                <h1>Accounts</h1>
                <MonacoEditor
                    height="600"
                    language="yaml"
                    theme="vs-dark"
                    value={code}
                    options={options}
                    onChange={change}
                />
                <button>
                    Submit
                </button>
            </div>
        );
    }
}