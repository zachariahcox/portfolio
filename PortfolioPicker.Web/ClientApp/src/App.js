import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Schema } from './components/Schema';
import { Picker } from './components/Picker';
import { Editor } from './components/Editor';

export default class App extends Component {
    displayName = App.name

    constructor(props) {
        super(props);
        this.state = {
            code: '// type your code...',
        };
        this.updateCode.bind(this);
    }

    updateCode(src) {
        this.setState({ code: src });
    }

    render() {
        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route path='/schema' component={Schema} />
                <Route path='/picker' component={Picker} />
                <Route path='/editor'
                    component={() => <Editor
                        code={this.state.code}
                        updateCode={this.updateCode} />} />
            </Layout>
        );
    }
}
