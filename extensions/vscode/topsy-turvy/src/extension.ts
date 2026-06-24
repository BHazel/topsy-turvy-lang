import { execFile } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { promisify } from 'util';
import * as vscode from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind,
} from 'vscode-languageclient/node';

import { resolveCliPath } from './paths.js';

const execFileAsync = promisify(execFile);

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

            const storedCommandLineArguments = context.workspaceState.get<string>(`commandLineArgs.${filePath}`, '');
            const performTiptoe = vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('performTiptoeMode', false);
            const cliArgs: string[] = ['perform', filePath];
            if (performTiptoe) {
                cliArgs.push('--tiptoe');
            }

            if (storedCommandLineArguments.trim().length > 0) {
                cliArgs.push('--');
                cliArgs.push(...storedCommandLineArguments.trim().split(/\s+/));
            }

            const task = new vscode.Task(
                { type: 'topsy-turvy-run' },
                vscode.TaskScope.Global,
                'Run Topsy Turvy File',
                'Topsy Turvy',
                new vscode.ProcessExecution(cliPath, cliArgs),
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
        vscode.commands.registerCommand('topsy-turvy.setCommandLineArgs', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            const filePath = editor.document.uri.fsPath;
            const current = context.workspaceState.get<string>(`commandLineArgs.${filePath}`, '');
            const result = await vscode.window.showInputBox({
                prompt: 'Command-line arguments (space-separated)',
                value: current,
                placeHolder: 'e.g. Ko-Ko "Pooh-Bah" 42',
            });

            if (result !== undefined) {
                await context.workspaceState.update(`commandLineArgs.${filePath}`, result);
            }
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

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.rehearseFile', async (uri?: vscode.Uri) => {
            const fileUri = uri ?? vscode.window.activeTextEditor?.document.uri;
            if (!fileUri) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            await vscode.workspace.save(fileUri);
            const filePath = fileUri.fsPath;
            const rehearseCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(rehearseCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${rehearseCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            const task = new vscode.Task(
                { type: 'topsy-turvy-rehearse' },
                vscode.TaskScope.Global,
                'Rehearse Topsy Turvy File',
                'Topsy Turvy',
                new vscode.ProcessExecution(rehearseCliPath, [
                'rehearse',
                filePath,
                ...(vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('rehearseTiptoeMode', false)
                    ? ['--tiptoe']
                    : []),
                ]),
            );

            task.presentationOptions = {
                reveal: vscode.TaskRevealKind.Always,
                focus: false,
                panel: vscode.TaskPanelKind.Shared,
                showReuseMessage: false,
                clear: true,
            };

            await vscode.tasks.executeTask(task);
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.startCadenza', () => {
            const cadenzaCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(cadenzaCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${cadenzaCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            const cadenzaTiptoe = vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('cadenzaTiptoeMode', false);
            const terminal = vscode.window.createTerminal({ name: 'Topsy Turvy REPL' });
            terminal.sendText(`"${cadenzaCliPath}" cadenza${cadenzaTiptoe ? ' --tiptoe' : ''}`);
            terminal.show();
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.commissionFile', async () => {
            const commissionCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(commissionCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${commissionCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            const title = await vscode.window.showInputBox({
                prompt: 'Programme title (required)',
                placeHolder: 'e.g. The Mikado',
            });

            if (!title?.trim()) {
                return;
            }

            const commissionSkipSubtitle = vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('commissionSkipSubtitle', false);
            let subtitle = '';
            if (!commissionSkipSubtitle) {
                const subtitleInput = await vscode.window.showInputBox({
                    prompt: 'Programme subtitle (optional — press Enter to skip)',
                    placeHolder: 'e.g. The Town of Titipu',
                });

                if (subtitleInput === undefined) {
                    return;
                }

                subtitle = subtitleInput;
            }

            const defaultUri = vscode.workspace.workspaceFolders?.[0]?.uri;
            const saveUri = await vscode.window.showSaveDialog({
                filters: { 'Topsy Turvy': ['topsy'] },
                defaultUri,
            });

            if (!saveUri) {
                return;
            }

            const args = ['commission', saveUri.fsPath, '--title', title.trim(), '--tiptoe'];
            if (subtitle.trim()) {
                args.push('--or', subtitle.trim());
            }

            try {
                await execFileAsync(commissionCliPath, args, { maxBuffer: 1_048_576 });
                const doc = await vscode.workspace.openTextDocument(saveUri);
                await vscode.window.showTextDocument(doc);
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                vscode.window.showErrorMessage(`Topsy Turvy: Commission failed — ${message}`);
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.cuePreProcessor', async (uri?: vscode.Uri) => {
            const fileUri = uri ?? vscode.window.activeTextEditor?.document.uri;
            if (!fileUri) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            await vscode.workspace.save(fileUri);
            const filePath = fileUri.fsPath;
            const cueCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(cueCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${cueCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            try {
                const { stdout } = await execFileAsync(
                    cueCliPath,
                    ['sorcerer', 'cue', filePath, '--tiptoe'],
                    { maxBuffer: 10 * 1024 * 1024 },
                );

                const doc = await vscode.workspace.openTextDocument({ content: stdout, language: 'topsy-turvy' });
                await vscode.window.showTextDocument(doc, { preview: false, viewColumn: vscode.ViewColumn.Beside });
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                vscode.window.showErrorMessage(`Topsy Turvy: Pre-processor failed — ${message}`);
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.mountProject', async () => {
            const mountCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(mountCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${mountCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            const defaultUri = vscode.workspace.workspaceFolders?.[0]?.uri;
            const parentFolderUris = await vscode.window.showOpenDialog({
                canSelectFolders: true,
                canSelectFiles: false,
                canSelectMany: false,
                openLabel: 'Select Parent Folder',
                defaultUri,
            });

            if (!parentFolderUris || parentFolderUris.length === 0) {
                return;
            }

            const dirName = await vscode.window.showInputBox({
                prompt: 'Project directory name (required)',
                placeHolder: 'e.g. my-operetta',
            });

            if (!dirName?.trim()) {
                return;
            }

            const projectTitle = await vscode.window.showInputBox({
                prompt: 'Programme title (required)',
                placeHolder: 'e.g. The Mikado',
            });

            if (!projectTitle?.trim()) {
                return;
            }

            const mountSkipSubtitle = vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('mountSkipSubtitle', false);
            let projectSubtitle = '';
            if (!mountSkipSubtitle) {
                const projectSubtitleInput = await vscode.window.showInputBox({
                    prompt: 'Programme subtitle (optional — press Enter to skip)',
                    placeHolder: 'e.g. The Town of Titipu',
                });

                if (projectSubtitleInput === undefined) {
                    return;
                }

                projectSubtitle = projectSubtitleInput;
            }

            const projectPath = path.join(parentFolderUris[0].fsPath, dirName.trim());
            const args = ['mount', projectPath, '--title', projectTitle.trim(), '--tiptoe'];
            if (projectSubtitle.trim()) {
                args.push('--or', projectSubtitle.trim());
            }

            try {
                await execFileAsync(mountCliPath, args, { maxBuffer: 1_048_576 });
                const choice = await vscode.window.showInformationMessage(
                    `Topsy Turvy: Project created at "${projectPath}".`,
                    'Open Folder',
                );

                if (choice === 'Open Folder') {
                    await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(projectPath));
                }
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                vscode.window.showErrorMessage(`Topsy Turvy: Mount failed — ${message}`);
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.generatePlaybill', async (uri?: vscode.Uri) => {
            const fileUri = uri ?? vscode.window.activeTextEditor?.document.uri;
            if (!fileUri) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            await vscode.workspace.save(fileUri);
            const filePath = fileUri.fsPath;
            const playbillCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(playbillCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${playbillCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            try {
                const { stdout } = await execFileAsync(
                    playbillCliPath,
                    ['playbill', filePath, '--tiptoe'],
                    { maxBuffer: 10 * 1024 * 1024 },
                );

                const playbillRender = vscode.workspace.getConfiguration('topsy-turvy').get<boolean>('playbillRenderMarkdown', true);
                const doc = await vscode.workspace.openTextDocument({ content: stdout, language: 'markdown' });
                await vscode.window.showTextDocument(doc, { preview: false });
                if (playbillRender) {
                    await vscode.commands.executeCommand('markdown.showPreview', doc.uri);
                }
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                vscode.window.showErrorMessage(`Topsy Turvy: Playbill generation failed — ${message}`);
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('topsy-turvy.viewAstPromptbook', async (uri?: vscode.Uri) => {
            const fileUri = uri ?? vscode.window.activeTextEditor?.document.uri;
            if (!fileUri) {
                vscode.window.showWarningMessage('Topsy Turvy: No active editor.');
                return;
            }

            await vscode.workspace.save(fileUri);
            const filePath = fileUri.fsPath;
            const promptbookCliPath = resolveConfiguredCliPath(context);
            if (!fs.existsSync(promptbookCliPath)) {
                vscode.window.showWarningMessage(
                    `Topsy Turvy: CLI not found at "${promptbookCliPath}". ` +
                        `Please build the project or set topsy-turvy.cliPath in settings.`,
                );

                return;
            }

            try {
                const { stdout } = await execFileAsync(
                    promptbookCliPath,
                    ['sorcerer', 'promptbook', filePath, '--tiptoe'],
                    { maxBuffer: 10 * 1024 * 1024 },
                );
                
                const doc = await vscode.workspace.openTextDocument({ content: stdout, language: 'json' });
                await vscode.window.showTextDocument(doc, { preview: false, viewColumn: vscode.ViewColumn.Beside });
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                vscode.window.showErrorMessage(`Topsy Turvy: AST Promptbook failed — ${message}`);
            }
        }),
    );
}

export function deactivate(): Thenable<void> | undefined {
    return client?.stop();
}
