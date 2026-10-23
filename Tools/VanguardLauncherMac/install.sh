#!/usr/bin/env bash
# Build the launcher from this checkout and install it as /Applications/VanguardLauncher.app,
# replacing whatever is there. One command to test a change without downloading a zip.
set -uo pipefail
cd "$(dirname "$0")"
./build.sh || true
APP="build/VanguardLauncher.app"
[ -d "$APP" ] || { echo "build/VanguardLauncher.app missing; build failed" >&2; exit 1; }
osascript -e 'quit app "VanguardLauncher"' >/dev/null 2>&1 || true
rm -rf "/Applications/VanguardLauncher.app"
ditto "$APP" "/Applications/VanguardLauncher.app"
echo "installed /Applications/VanguardLauncher.app ($(/usr/libexec/PlistBuddy -c 'Print CFBundleShortVersionString' /Applications/VanguardLauncher.app/Contents/Info.plist))"
open "/Applications/VanguardLauncher.app"
