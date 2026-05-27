import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind,
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: vscode.ExtensionContext): void {
    const config = vscode.workspace.getConfiguration('topsy-turvy');
    const configuredPath = config.get<string>('serverPath');
    const buildConfiguration = config.get<string>('buildConfiguration') || 'Debug';

    const defaultPath = context.asAbsolutePath(
        path.join(
            '..', '..', '..', 'interpreter',
            'BWHazel.TopsyTurvy.LanguageServer',
            'bin', buildConfiguration, 'net10.0',
            'BWHazel.TopsyTurvy.LanguageServer'
        )
    );

    const serverPath = configuredPath && configuredPath.length > 0
        ? configuredPath
        : defaultPath;

    if (!fs.existsSync(serverPath)) {
        vscode.window.showWarningMessage(
            `Topsy Turvy: language server not found at "${serverPath}". ` +
            `Build the project or set topsy-turvy.serverPath in settings.`
        );
        return;
    }

    const serverOptions: ServerOptions = {
        command: serverPath,
        transport: TransportKind.stdio,
    };

    const clientOptions: LanguageClientOptions = {
        documentSelector: [{
            scheme: 'file',
            language: 'topsy-turvy'
        }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.topsy'),
        },
    };

    client = new LanguageClient(
        'topsy-turvy',
        'Topsy Turvy Language Server',
        serverOptions,
        clientOptions
    );

    client.start();
    context.subscriptions.push(client);

    context.subscriptions.push(
        vscode.commands.registerCommand(
            'topsy-turvy.showReferences',
            async (uriString: string, line: number, character: number) => {
                const uri = vscode.Uri.parse(uriString);
                const position = new vscode.Position(line, character);
                await vscode.commands.executeCommand(
                    'editor.action.findReferences',
                    uri,
                    position
                );
            }
        )
    );
}

export function deactivate(): Thenable<void> | undefined {
    return client?.stop();
}
