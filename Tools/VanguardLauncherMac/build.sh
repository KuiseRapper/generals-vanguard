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
#
# Builds the Generals Vanguard macOS launcher app and DMG.
set -euo pipefail
cd "$(dirname "$0")"
# The sandbox and some CI shells do not allow the default module/temporary cache
# under /var/folders; keep Swift's caches beside the source instead.
export SWIFT_MODULE_CACHE_PATH="$PWD/.swift-cache"
export CLANG_MODULE_CACHE_PATH="$PWD/.swift-cache/clang"
export TMPDIR="$PWD/.tmp"
mkdir -p "$SWIFT_MODULE_CACHE_PATH" "$TMPDIR"
rm -rf build
APP="build/VanguardLauncher.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
swiftc -O -parse-as-library -framework SwiftUI -framework AppKit -o "$APP/Contents/MacOS/VanguardLauncher" main.swift
cp Info.plist "$APP/Contents/Info.plist"
cp ../VanguardLauncher/Assets/Background.png "$APP/Contents/Resources/Background.png"
# App icon: the launcher's own star icon, expanded for macOS.
rm -rf build/icon-work && mkdir -p build/icon-work/AppIcon.iconset
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 32x32 build/icon-work/AppIcon.iconset/icon_16x16@2x.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 16x16 build/icon-work/AppIcon.iconset/icon_16x16.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 64x64 build/icon-work/AppIcon.iconset/icon_32x32@2x.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 32x32 build/icon-work/AppIcon.iconset/icon_32x32.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 256x256 build/icon-work/AppIcon.iconset/icon_128x128@2x.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 128x128 build/icon-work/AppIcon.iconset/icon_128x128.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 512x512 build/icon-work/AppIcon.iconset/icon_256x256@2x.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 256x256 build/icon-work/AppIcon.iconset/icon_256x256.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 1024x1024 build/icon-work/AppIcon.iconset/icon_512x512@2x.png
magick "../VanguardLauncher/Assets/Vanguard.ico[0]" -resize 512x512 build/icon-work/AppIcon.iconset/icon_512x512.png
iconutil -c icns -o "$APP/Contents/Resources/AppIcon.icns" build/icon-work/AppIcon.iconset
codesign --force --deep --sign - "$APP"
rm -rf build/dmg
mkdir -p build/dmg
cp -R "$APP" build/dmg/
ln -s /Applications build/dmg/Applications
hdiutil create -volname "Generals Vanguard" -srcfolder build/dmg -ov -format UDZO build/VanguardLauncher-mac.dmg
echo "built $APP and build/VanguardLauncher-mac.dmg"
