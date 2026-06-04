import * as fs from 'fs';
import * as vscode from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind,
} from 'vscode-languageclient/node';

import { resolveCliPath } from './paths.js';

let client: LanguageClient;
let runTaskExecution: vscode.TaskExecution | undefined;

function resolveConfiguredCliPath(context: vscode.ExtensionContext): string {
    const config = vscode.workspace.getConfiguration('topsy-turvy');
    const configuredPath = config.get<string>('cliPath');
    return resolveCliPath(configuredPath, context.extensionPath);
}

export function activate(context: vscode.ExtensionContext): void {
    const cliPath = resolveConfiguredCliPath(context);

    if (!fs.existsSync(cliPath)) {
        vscode.window.showWarningMessage(
            `Topsy Turvy: CLI not found at "${cliPath}". ` +
                `Build the project or set topsy-turvy.cliPath in settings.`,
        );
        return;
    }

    const serverOptions: ServerOptions = {
        command: cliPath,
        args: ['sorcerer', 'incantation'],
        transport: TransportKind.stdio,
    };

    const clientOptions: LanguageClientOptions = {
        documentSelector: [
            {
                scheme: 'file',
                language: 'topsy-turvy',
            },
        ],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.topsy'),
        },
    };

    client = new LanguageClient(
        'topsy-turvy',
        'Topsy Turvy Language Server',
        serverOptions,
        clientOptions,
    );

    client.start();
    context.subscriptions.push(client);

    context.subscriptions.push(
        vscode.commands.registerCommand(
            'topsy-turvy.showReferences',
            async (uriString: string, line: number, character: number) => {
                const uri = vscode.Uri.parse(uriString);
                const position = new vscode.Position(line, character);
                await vscode.commands.executeCommand('editor.action.findReferences', uri, position);
            },
        ),
    );

    context.subscriptions.push(
        vscode.tasks.onDidEndTaskProcess((e) => {
            if (e.execution === runTaskExecution) {
                runTaskExecution = undefined;
                void vscode.commands.executeCommand('setContext', 'topsyTurvyRunning', false);
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.runFile', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            await editor.document.save();
            const filePath = editor.document.uri.fsPath;

            const cliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(cliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${cliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            const task = new vscode.Task(
                { type: 'topsy-turvy-run' },
                vscode.TaskScope.Global,
                'Run Topsy Turvy File',
                'Topsy Turvy',
                new vscode.ProcessExecution(cliPath, ['perform', filePath]),
            );
            task.presentationOptions = {
                reveal: vscode.TaskRevealKind.Always,
                focus: false,
                panel: vscode.TaskPanelKind.Shared,
                showReuseMessage: false,
                clear: true,
            };

            runTaskExecution = await vscode.tasks.executeTask(task);
            await vscode.commands.executeCommand('setContext', 'topsyTurvyRunning', true);
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.stopFile', () => {
            if (runTaskExecution) {
                runTaskExecution.terminate();
                runTaskExecution = undefined;
                void vscode.commands.executeCommand('setContext', 'topsyTurvyRunning', false);
            }
        }),
    );
}

export function deactivate(): Thenable<void> | undefined {
    return client?.stop();
}
