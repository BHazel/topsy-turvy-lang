#!/usr/bin/env node

/**
 * Publishes the Topsy Turvy CLI as a self-contained, single-file binary into
 * the extension bin/ directory for inclusion in the packaged .vsix file.
 *
 * Run via: npm run prebuild:dotnet
 */

import { execSync } from 'child_process';
import { rmSync } from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const extensionRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const cliProjectPath = path.resolve(extensionRoot, '..', '..', '..', 'interpreter', 'BWHazel.TopsyTurvy.Cli');
const outputDirectory = path.join(extensionRoot, 'bin');

function resolveRid() {
    const platform = process.platform;
    const arch = process.arch;

    if (platform === 'win32') {
        return arch === 'arm64'
            ? 'win-arm64'
            : 'win-x64';
    }
    if (platform === 'darwin') {
        return arch === 'arm64'
            ? 'osx-arm64'
            : 'osx-x64';
    }
    return arch === 'arm64'
        ? 'linux-arm64'
        : 'linux-x64';
}

const rid = resolveRid();

console.log(`Cleaning ${outputDirectory}`);
rmSync(outputDirectory, {
    recursive: true,
    force: true
});

const command = [
    'dotnet', 'publish',
    `"${cliProjectPath}"`,
    '--configuration', 'Release',
    '--runtime', rid,
    '--self-contained', 'true',
    '--output', `"${outputDirectory}"`,
].join(' ');

console.log(`Publishing CLI for ${rid} → ${outputDirectory}`);
console.log(`  ${command}`);

execSync(command, {
    stdio: 'inherit'
});
