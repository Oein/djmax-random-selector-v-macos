#!/usr/bin/env bash
#
# Packages a published .NET app into a macOS .app bundle and a compressed .dmg.
#
# Usage: make-dmg.sh <rid> <publish-dir> <dist-dir>
#   <rid>          runtime identifier, e.g. osx-arm64 / osx-x64
#   <publish-dir>  directory containing the `dotnet publish` output
#   <dist-dir>     output directory for the .app and .dmg
#
set -euo pipefail

RID="${1:?rid required}"
PUBLISH_DIR="${2:?publish dir required}"
DIST_DIR="${3:?dist dir required}"

APP_NAME="DJMAX Random Selector V"
EXECUTABLE="DJMAX Random Selector V"   # must match <AssemblyName> of the Desktop project
BUNDLE_ID="com.pali-fly.djmax-random-selector-v"
VERSION="${APP_VERSION:-1.0.0}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ICON_SRC="$ROOT/DjmaxRandomSelectorV/Images/icon2.png"

APP_DIR="$DIST_DIR/$APP_NAME.app"

rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# --- payload -----------------------------------------------------------------
cp -R "$PUBLISH_DIR/." "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/$EXECUTABLE"

# --- icon (png -> icns) ------------------------------------------------------
if [[ -f "$ICON_SRC" ]]; then
  ICONSET="$(mktemp -d)/icon.iconset"
  mkdir -p "$ICONSET"
  for size in 16 32 64 128 256 512; do
    sips -z "$size" "$size" "$ICON_SRC" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
    dbl=$((size * 2))
    sips -z "$dbl" "$dbl" "$ICON_SRC" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET" -o "$APP_DIR/Contents/Resources/icon.icns"
fi

# --- Info.plist --------------------------------------------------------------
cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleExecutable</key><string>$EXECUTABLE</string>
  <key>CFBundleIconFile</key><string>icon</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSPrincipalClass</key><string>NSApplication</string>
</dict>
</plist>
PLIST

# --- ad-hoc code signature ---------------------------------------------------
# Required so the app is launchable on Apple Silicon (unsigned arm64 binaries are
# killed by the kernel). This is NOT notarized: users still right-click > Open.
codesign --force --deep --sign - "$APP_DIR"

# --- dmg ---------------------------------------------------------------------
mkdir -p "$DIST_DIR"
DMG_PATH="$DIST_DIR/DJMAX-Random-Selector-V-$RID.dmg"
rm -f "$DMG_PATH"

STAGING="$(mktemp -d)"
cp -R "$APP_DIR" "$STAGING/"
ln -s /Applications "$STAGING/Applications"

hdiutil create \
  -volname "$APP_NAME" \
  -srcfolder "$STAGING" \
  -fs HFS+ \
  -format UDZO \
  -ov \
  "$DMG_PATH"

echo "Created $DMG_PATH"
