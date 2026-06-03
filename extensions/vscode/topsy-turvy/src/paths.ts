import * as path from 'path';

/**
 * Resolves the absolute path to the CLI binary.
 * @param cliPathSetting The user-configured path to the CLI binary.
 * @param buildConfig The build configuration of the CLI binary.
 * @param extensionPath The path to the extension directory.
 *
 * When {@link cliPathSetting} is non-empty it is returned unchanged.
 * Otherwise the default path is computed relative to {@link extensionPath}
 * using {@link buildConfig} to select the build artefacts.
 */
export function resolveCliPath(
    cliPathSetting: string | undefined,
    buildConfig: string,
    extensionPath: string,
): string {
    const binaryName = process.platform === 'win32'
        ? 'operetta.exe'
        : 'operetta';
    
    const defaultPath = path.resolve(
        extensionPath,
        '..',
        '..',
        '..',
        'interpreter',
        'BWHazel.TopsyTurvy.Cli',
        'bin',
        buildConfig,
        'net10.0',
        binaryName,
    );

    return cliPathSetting && cliPathSetting.length > 0
        ? cliPathSetting
        : defaultPath;
}

/**
 * Resolves the absolute path to the language server executable.
 * @param serverPathSetting The user-configured path to the language server executable.
 * @param buildConfig The build configuration of the language server executable.
 * @param extensionPath The path to the extension directory.
 *
 * When {@link serverPathSetting} is non-empty it is returned unchanged.
 * Otherwise the default path is computed relative to {@link extensionPath}
 * using {@link buildConfig} to select the build artefacts.
 */
export function resolveServerPath(
    serverPathSetting: string | undefined,
    buildConfig: string,
    extensionPath: string,
): string {
    const defaultPath = path.resolve(
        extensionPath,
        '..',
        '..',
        '..',
        'interpreter',
        'BWHazel.TopsyTurvy.LanguageServer',
        'bin',
        buildConfig,
        'net10.0',
        'BWHazel.TopsyTurvy.LanguageServer',
    );
    
    return serverPathSetting && serverPathSetting.length > 0 ? serverPathSetting : defaultPath;
}
