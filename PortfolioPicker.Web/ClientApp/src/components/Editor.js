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
        this.accountsEditor = null;
        this.fundsEditor = null;
    }
    componentWillUnmount() {
        if (this.accountsEditor) {
            this.props.cacheAccounts(this.accountsEditor.getValue());
        }
        if (this.fundsEditor) {
            this.props.cacheFunds(this.fundsEditor.getValue());
        }
    }

    mountAccounts(editor, monaco) {
        editor.focus();
        this.accountsEditor = editor;
    }
    mountFunds(editor, monaco) {
        editor.focus();
        this.fundsEditor = editor;
    }
    setAccounts(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.accountsEditor.setValue(reader.result);
        };
        reader.readAsText(file);
    }
    setFunds(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.fundsEditor.setValue(reader.result);
        };
        reader.readAsText(file);
    }


    render() {
        const options = {
            selectOnLineNumbers: true
        };
        return (
            <div>
                <h1>Accounts</h1>
                <input
                    type="file"
                    accept=".yaml"
                    onChange={e => this.setAccounts.bind(this)(e.target.files[0])}
                />
                <MonacoEditor
                    height="300"
                    language="yaml"
                    theme="vs-dark"
                    value={this.props.accountsYaml}
                    options={options}
                    editorDidMount={this.mountAccounts.bind(this)}
                />

                <h1>Funds List</h1>
                <input
                    type="file"
                    accept=".yaml"
                    onChange={e => this.setFunds.bind(this)(e.target.files[0])}
                />
                <MonacoEditor
                    height="300"
                    language="yaml"
                    theme="vs-dark"
                    value={this.props.fundsYaml}
                    options={options}
                    editorDidMount={this.mountFunds.bind(this)}
                />
            </div>
        );
    }
}