import * as vscode from 'vscode';

/**
 * Spawns `director --adapter` as the DAP server for a debug session.
 */
export class TopsyTurvyDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
    /**
     * Initialises a new instance of the `TopsyTurvyDebugAdapterDescriptorFactory` class.
     * @param resolveCliPath A function that returns the path to the Topsy Turvy CLI executable.
     */
    public constructor(private readonly resolveCliPath: () => string) {}

    /**
     * Creates a `DebugAdapterDescriptor` for a debug session, which spawns `director --adapter` as the DAP server.
     * @param _session The debug session for which the descriptor is being created.
     * @param _executable The debug adapter executable, if any, that was specified in the launch configuration.
     * @returns A `DebugAdapterDescriptor` that spawns `director --adapter` as the DAP server.
     */
    public createDebugAdapterDescriptor(
        _session: vscode.DebugSession,
        _executable: vscode.DebugAdapterExecutable | undefined,
    ): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
        return new vscode.DebugAdapterExecutable(this.resolveCliPath(), ['director', '--adapter']);
    }
}

/**
 * Logs every DAP message sent to and received from `director --adapter` to a dedicated Output channel.
 */
export class TopsyTurvyDebugAdapterTrackerFactory implements vscode.DebugAdapterTrackerFactory {
    private readonly outputChannel = vscode.window.createOutputChannel('Topsy Turvy Debugger');

    /**
     * Creates a `DebugAdapterTracker` for a debug session.
     * @param session The debug session for which the tracker is being created.
     * @returns A `DebugAdapterTracker` that logs every DAP message sent to and received from `director --adapter` to a dedicated Output channel.
     */
    public createDebugAdapterTracker(session: vscode.DebugSession): vscode.ProviderResult<vscode.DebugAdapterTracker> {
        /**
         * Appends one line to the Output channel, prefixed with the session ID and message direction.
         * @param direction A label for the kind of event being logged.
         * @param message The message or payload to log as stringified JSON.
         */
        const log = (direction: string, message: unknown): void => {
            this.outputChannel.appendLine(`[${session.id}] ${direction} ${JSON.stringify(message)}`);
        };

        return {
            onWillStartSession: (): void => log('start', {
                name: session.name,
                configuration: session.configuration
            }),
            onWillReceiveMessage: (message: unknown): void => log('->', message),
            onDidSendMessage: (message: unknown): void => log('<-', message),
            onError: (error: Error): void => log('error', error.message),
            onExit: (code: number | undefined, signal: string | undefined): void => log('exit', { code, signal }),
        };
    }
}

/**
 * Defaults a launch configuration's `program` to the active editor's file when omitted.
 * 
 * This ensures pressing F5 with no launch.json still works.
 */
export class TopsyTurvyDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    /**
     * Fills in a launch configuration's `program` when omitted, defaulting to the active editor's file.
     * @param _folder The workspace folder the configuration was resolved from, if any.
     * @param config The launch configuration to resolve.
     * @param _token A token indicating the resolution has been cancelled.
     * @returns The resolved configuration, or `undefined` if there is no file to debug.
     */
    public resolveDebugConfiguration(
        _folder: vscode.WorkspaceFolder | undefined,
        config: vscode.DebugConfiguration,
        _token?: vscode.CancellationToken,
    ): vscode.ProviderResult<vscode.DebugConfiguration> {
        if (!config.type && !config.request && !config.name) {
            const editor = vscode.window.activeTextEditor;
            if (editor && editor.document.languageId === 'topsy-turvy') {
                config.type = 'topsy-turvy';
                config.request = 'launch';
                config.name = 'Topsy Turvy File';
                config.program = editor.document.uri.fsPath;
            }
        }

        if (!config.program) {
            const editor = vscode.window.activeTextEditor;
            config.program = editor?.document.uri.fsPath;
        }

        if (!config.program) {
            vscode.window.showWarningMessage('Topsy Turvy: No active editor to debug.');
            return undefined;
        }

        return config;
    }
}
