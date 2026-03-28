#!/usr/bin/env bash
set -Eeuo pipefail

log() {
  printf '==> %s\n' "$1"
}

fail() {
  local exit_code=$?
  printf 'build-macos.sh failed at line %s with exit code %s\n' "${BASH_LINENO[0]}" "$exit_code" >&2
  exit "$exit_code"
}

trap fail ERR

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd -P)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/.." >/dev/null 2>&1 && pwd -P)"
SWIFT_PROJECT_DIR="$PROJECT_ROOT/macos/CopySaveMac"
OUTPUT_DIR="$PROJECT_ROOT/dist/macos"
SUPPORT_DIR="$SWIFT_PROJECT_DIR/Support"
PRODUCT_VERSION="${PRODUCT_VERSION:-1.0.0}"
APP_NAME="CopySave"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"
APP_CONTENTS="$APP_BUNDLE/Contents"
APP_MACOS_DIR="$APP_CONTENTS/MacOS"
APP_RESOURCES_DIR="$APP_CONTENTS/Resources"
APP_FRAMEWORKS_DIR="$APP_CONTENTS/Frameworks"
ICONSET_DIR="$OUTPUT_DIR/AppIcon.iconset"
DMG_STAGING_DIR="$OUTPUT_DIR/dmg-root"
DMG_PATH="$OUTPUT_DIR/$APP_NAME-$PRODUCT_VERSION.dmg"
ICON_SOURCE_PNG="$OUTPUT_DIR/AppIcon.svg.png"

log "Preparing output directories"
mkdir -p "$OUTPUT_DIR"
rm -rf "$APP_BUNDLE" "$ICONSET_DIR" "$DMG_STAGING_DIR" "$DMG_PATH" "$ICON_SOURCE_PNG"

log "Building Swift package"
cd "$SWIFT_PROJECT_DIR"
swift build -c release --product CopySaveMac

log "Creating app bundle"
mkdir -p "$APP_MACOS_DIR" "$APP_RESOURCES_DIR" "$APP_FRAMEWORKS_DIR"
cp ".build/release/CopySaveMac" "$APP_MACOS_DIR/$APP_NAME"
chmod +x "$APP_MACOS_DIR/$APP_NAME"
sed "s/__VERSION__/$PRODUCT_VERSION/g" "$SUPPORT_DIR/Info.plist.template" > "$APP_CONTENTS/Info.plist"

if command -v qlmanage >/dev/null 2>&1 && command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
  log "Generating app icon"
  if qlmanage -t -s 1024 -o "$OUTPUT_DIR" "$SUPPORT_DIR/AppIcon.svg" >/dev/null 2>&1 && [[ -f "$ICON_SOURCE_PNG" ]]; then
    mkdir -p "$ICONSET_DIR"
    sips -z 16 16 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16.png" >/dev/null
    sips -z 32 32 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png" >/dev/null
    sips -z 32 32 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32.png" >/dev/null
    sips -z 64 64 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png" >/dev/null
    sips -z 128 128 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128.png" >/dev/null
    sips -z 256 256 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null
    sips -z 256 256 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256.png" >/dev/null
    sips -z 512 512 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null
    sips -z 512 512 "$ICON_SOURCE_PNG" --out "$ICONSET_DIR/icon_512x512.png" >/dev/null
    cp "$ICON_SOURCE_PNG" "$ICONSET_DIR/icon_512x512@2x.png"
    iconutil -c icns "$ICONSET_DIR" -o "$APP_RESOURCES_DIR/AppIcon.icns"
  else
    log "Skipping icon generation because SVG preview conversion failed"
  fi
else
  log "Skipping icon generation because macOS icon tools are unavailable"
fi

rm -f "$ICON_SOURCE_PNG"
rm -rf "$ICONSET_DIR"

if xcrun --find swift-stdlib-tool >/dev/null 2>&1; then
  log "Embedding Swift runtime"
  xcrun swift-stdlib-tool \
    --copy \
    --scan-executable "$APP_MACOS_DIR/$APP_NAME" \
    --destination "$APP_FRAMEWORKS_DIR" \
    --platform macosx
else
  log "Skipping Swift runtime embedding because swift-stdlib-tool is unavailable"
fi

if command -v codesign >/dev/null 2>&1; then
  log "Applying ad-hoc code signature"
  codesign --force --deep --sign - "$APP_BUNDLE"
fi

log "Creating DMG"
mkdir -p "$DMG_STAGING_DIR"
cp -R "$APP_BUNDLE" "$DMG_STAGING_DIR/$APP_NAME.app"
ln -s /Applications "$DMG_STAGING_DIR/Applications"
hdiutil create \
  -volname "$APP_NAME" \
  -srcfolder "$DMG_STAGING_DIR" \
  -ov \
  -format UDZO \
  "$DMG_PATH"

rm -rf "$DMG_STAGING_DIR"

log "Built app bundle: $APP_BUNDLE"
log "Built dmg: $DMG_PATH"
