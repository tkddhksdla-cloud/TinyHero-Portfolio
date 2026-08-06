#!/bin/sh

set -eu

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <Unity arguments...>" >&2
    exit 2
fi

if [ -z "${IOS_UNITY_EXE:-}" ]; then
    echo "IOS_UNITY_EXE is not configured." >&2
    exit 2
fi

if [ ! -x "$IOS_UNITY_EXE" ]; then
    echo "Unity executable was not found: $IOS_UNITY_EXE" >&2
    exit 2
fi

log_file=""
previous_argument=""

for argument in "$@"; do
    if [ "$previous_argument" = "-logFile" ]; then
        log_file="$argument"
        break
    fi

    previous_argument="$argument"
done

if [ -z "$log_file" ]; then
    echo "Unity arguments must include -logFile <path>." >&2
    exit 2
fi

log_directory=$(dirname "$log_file")
mkdir -p "$log_directory"

heartbeat_pid=""
tail_pid=""

cleanup() {
    if [ -n "$heartbeat_pid" ]; then
        kill "$heartbeat_pid" 2>/dev/null || true
    fi

    if [ -n "$tail_pid" ]; then
        kill "$tail_pid" 2>/dev/null || true
    fi
}

trap cleanup EXIT INT TERM

echo "========== TinyHero iOS Unity Process =========="
echo "Unity: $IOS_UNITY_EXE"
echo "Log: $log_file"
echo "================================================"

tail -n +1 -F "$log_file" &
tail_pid=$!

"$IOS_UNITY_EXE" "$@" &
unity_pid=$!

(
    while kill -0 "$unity_pid" 2>/dev/null; do
        printf '\n[TinyHero Build] iOS Unity is running. pid=%s\n' "$unity_pid"
        sleep 15
    done
) &
heartbeat_pid=$!

set +e
wait "$unity_pid"
unity_exit_code=$?
set -e

exit "$unity_exit_code"
