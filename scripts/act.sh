#!/usr/bin/env bash
# Wrapper script to run `act`` for testing GitHub Actions.

# Script fails on any error, unset variable, or failed pipe command.
set -euo pipefail

# Resolves the container architecture from the host CPU.
case "$(uname -m)" in
    arm64|aarch64) ARCH="linux/arm64" ;;
    *)             ARCH="linux/amd64" ;;
esac

# Holds Podman socket when Podman Desktop is in use.
# Ignored if Docker Desktop is in use.
SOCKET_ARGS=()

# Checks for Podman Desktop and sets socket path if found.
if command -v podman &>/dev/null; then
    SOCKET_PATH=$(podman machine inspect --format '{{.ConnectionInfo.PodmanSocket.Path}}' 2>/dev/null || true)
    if [[ -n "$SOCKET_PATH" ]]; then
        SOCKET_ARGS=(--container-daemon-socket "unix://${SOCKET_PATH}")
    fi
fi

# Passes architecture, socket argument and all others to `act`.
exec act --container-architecture "${ARCH}" "${SOCKET_ARGS[@]}" "$@"
