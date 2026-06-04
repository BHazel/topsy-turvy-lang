import * as fs from 'fs';
import * as path from 'path';

/**
 * Resolves the absolute path to the CLI binary.
 * @param cliPathSetting The user-configured path to the CLI binary.
 * @param extensionPath The path to the extension directory.
 *
 * Checks for the CLI binary at {@link extensionPath}/bin, produced by
 * `npm run prebuild:dotnet` but if not found, falls back to the Debug
 * build in the dev repository layout.
 */
export function resolveCliPath(
    cliPathSetting: string | undefined,
    extensionPath: string,
): string {
    const binaryName = process.platform === 'win32'
        ? 'operetta.exe'
        : 'operetta';

    const bundledPath = path.join(extensionPath, 'bin', binaryName);
    const devLayoutPath = path.resolve(
        extensionPath,
        '..',
        '..',
        '..',
        'interpreter',
        'BWHazel.TopsyTurvy.Cli',
        'bin',
        'Debug',
        'net10.0',
        binaryName,
    );

    const defaultPath = fs.existsSync(bundledPath)
        ? bundledPath
        : devLayoutPath;

    return cliPathSetting && cliPathSetting.length > 0
        ? cliPathSetting
        : defaultPath;
}
