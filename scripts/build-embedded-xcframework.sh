#!/usr/bin/env bash
#
# Publishes BWHazel.TopsyTurvy.Embedded for iOS runtime identifiers and combines the dylib slices into a
# single XCFramework for hand-rolled Xcode consumption.

# Script fails on any error, unset variable, or failed pipe command.
set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EMBEDDED_PROJECT="${REPOSITORY_ROOT}/operetta/BWHazel.TopsyTurvy.Embedded/BWHazel.TopsyTurvy.Embedded.csproj"
PUBLISH_ROOT="${REPOSITORY_ROOT}/operetta/BWHazel.TopsyTurvy.Embedded/bin/Release/net10.0"
SOURCE_HEADERS_DIR="${REPOSITORY_ROOT}/operetta/BWHazel.TopsyTurvy.Embedded/include"
HEADERS_DIR="${REPOSITORY_ROOT}/apps/apple/TopsyTurvy/Frameworks/include"
OUTPUT_DIR="${REPOSITORY_ROOT}/apps/apple/TopsyTurvy/Frameworks"
OUTPUT_XCFRAMEWORK="${OUTPUT_DIR}/TopsyTurvyToolchain.xcframework"

RIDS=("ios-arm64" "iossimulator-arm64")

for RID in "${RIDS[@]}"; do
    echo "Publishing ${RID}..."
    dotnet publish "${EMBEDDED_PROJECT}" -r "${RID}" -c Release
done

# Resolves the published dylib path for a runtime identifier dynamically, rather than hard-coding
# the output filename, since a future .NET version may prepend "lib" to Unix native library output.
resolve_dylib() {
    local rid="$1"
    find "${PUBLISH_ROOT}/${rid}/publish" -maxdepth 1 -name "*.dylib" -print -quit
}

IOS_ARM64_DYLIB="$(resolve_dylib ios-arm64)"
IOSSIMULATOR_ARM64_DYLIB="$(resolve_dylib iossimulator-arm64)"

# Verify that all dylibs were found, otherwise exit with an error.
if [[ -z "${IOS_ARM64_DYLIB}" || -z "${IOSSIMULATOR_ARM64_DYLIB}" ]]; then
    echo "Could not locate a published dylib for one or more runtime identifiers." >&2
    exit 1
fi

# Stages a fresh copy of the canonical, portable C headers (owned by the Embedded project, not this app)
# into the location xcodebuild reads from.  Only the copied header tree is refreshed here: module.modulemap
# lives directly in HEADERS_DIR and is hand-maintained, since it is Clang/Xcode packaging metadata for this
# one Apple consumer, not a portable C artefact the Embedded project should own.
mkdir -p "${HEADERS_DIR}"
rm -rf "${HEADERS_DIR}/topsyturvy.h" "${HEADERS_DIR}/topsyturvy"
cp -R "${SOURCE_HEADERS_DIR}/topsyturvy.h" "${SOURCE_HEADERS_DIR}/topsyturvy" "${HEADERS_DIR}/"

rm -rf "${OUTPUT_XCFRAMEWORK}"

# Create the XCFramework from the dylib slices and their headers.
xcodebuild -create-xcframework \
    -library "${IOS_ARM64_DYLIB}" -headers "${HEADERS_DIR}" \
    -library "${IOSSIMULATOR_ARM64_DYLIB}" -headers "${HEADERS_DIR}" \
    -output "${OUTPUT_XCFRAMEWORK}"

echo "Built ${OUTPUT_XCFRAMEWORK}"
