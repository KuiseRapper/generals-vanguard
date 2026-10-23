#!/usr/bin/env bash
#	Copyright 2026 KuiseRapper for Generals Vanguard.
#	Additional terms under GNU GPL section 7 apply: see LICENSE.md.
#
#	This program is free software: you can redistribute it and/or modify
#	it under the terms of the GNU General Public License as published by
#	the Free Software Foundation, either version 3 of the License, or
#	(at your option) any later version.
#
#	This program is distributed in the hope that it will be useful,
#	but WITHOUT ANY WARRANTY; without even the implied warranty of
#	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#	GNU General Public License for more details.
#
#	You should have received a copy of the GNU General Public License
#	along with this program.  If not, see <http://www.gnu.org/licenses/>.
#
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
