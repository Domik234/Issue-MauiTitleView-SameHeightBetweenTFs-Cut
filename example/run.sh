#!/bin/zsh
# Builds and runs the repro on an iOS simulator for the chosen target framework, prints the native layout and saves a screenshot.
#
#   ./run.sh net9            .NET 9  (Xcode 26.5)
#   ./run.sh net10           .NET 10 (Xcode 27.1)
#   ./run.sh net10 --push    also pushes a second page and prints its layout
#
# Environment (optional):
#   XCODE_NET9, XCODE_NET10   paths to Xcode.app for each target framework
#   SIM_RUNTIME               simulator runtime, default iOS-27-0
#   SIM_NAME                  simulator name, default "TitleViewRepro <SIM_TYPE> (<runtime>)"
#   SIM_TYPE                  device type used when creating the simulator, default iPhone-14
#   SIM_UDID                  a specific simulator (otherwise SIM_NAME is used or created)
#   MAUI_NET9, MAUI_NET10     MAUI version (passed as MauiVersionNet9 / MauiVersionNet10)
set -euo pipefail
cd "$(dirname "$0")"

case "${1:-}" in
  net9)  TF=net9.0-ios;  XCODE="${XCODE_NET9:-/Applications/Xcode-26.5.0.app}" ;;
  net10) TF=net10.0-ios; XCODE="${XCODE_NET10:-/Applications/Xcode-27.1.0.app}" ;;
  *) echo "usage: ./run.sh net9|net10 [--push]"; exit 1 ;;
esac
export DEVELOPER_DIR="$XCODE/Contents/Developer"
PUSH=0; [[ "${2:-}" == "--push" ]] && PUSH=1

RUNTIME="${SIM_RUNTIME:-iOS-27-0}"
SIM_TYPE="${SIM_TYPE:-iPhone-14}"
SIM_NAME="${SIM_NAME:-TitleViewRepro ${SIM_TYPE} (${RUNTIME})}"
# A regular iPhone (390×844 @3x) for the given runtime; the device is created if missing. On the machine this was developed on,
# the iOS 27.1 runtime only has the iPhone Duo (SIM_TYPE=iPhone-Duo).
# Delete it with: xcrun simctl delete <udid>
UDID="${SIM_UDID:-$(xcrun simctl list devices available -j | SIM_NAME="$SIM_NAME" RUNTIME="$RUNTIME" python3 -I -c "
import json, os, sys
d = json.load(sys.stdin)['devices']
for rt, devs in d.items():
    if rt.endswith(os.environ['RUNTIME']):
        for dev in devs:
            if dev['name'] == os.environ['SIM_NAME']:
                print(dev['udid']); sys.exit(0)
")}"
if [[ -z "$UDID" ]]; then
  echo ">> creating simulator '$SIM_NAME'"
  UDID="$(xcrun simctl create "$SIM_NAME" com.apple.CoreSimulator.SimDeviceType.$SIM_TYPE "com.apple.CoreSimulator.SimRuntime.$RUNTIME")"
fi

PROPS=()
[[ -n "${MAUI_NET9:-}" ]]  && PROPS+=("-p:MauiVersionNet9=$MAUI_NET9")
[[ -n "${MAUI_NET10:-}" ]] && PROPS+=("-p:MauiVersionNet10=$MAUI_NET10")

echo ">> build $TF (Xcode: $XCODE)"
dotnet build -f "$TF" -c Debug -p:RuntimeIdentifier=iossimulator-arm64 "${PROPS[@]}" -v:q -nologo

APP="bin/Debug/$TF/iossimulator-arm64/TitleViewRepro.app"
mkdir -p "$PWD/bin"
OUT="$PWD/bin/$1-dump.txt"; SHOT="$PWD/bin/$1.png"; SHOT_PUSH="$PWD/bin/$1-push.png"
rm -f "$OUT"

echo ">> simulator $UDID"
xcrun simctl boot "$UDID" 2>/dev/null || true
xcrun simctl bootstatus "$UDID" -b >/dev/null
xcrun simctl install "$UDID" "$APP"
xcrun simctl terminate "$UDID" com.example.titleviewrepro 2>/dev/null || true

export SIMCTL_CHILD_TITLEVIEW_AUTODUMP=1
(( PUSH )) && export SIMCTL_CHILD_TITLEVIEW_AUTOPUSH=1
xcrun simctl launch --stdout="$OUT" --stderr="$OUT" "$UDID" com.example.titleviewrepro >/dev/null

# Waits until the given string appears in the output (the first start after an install can take several seconds)
wait_for() {
  for i in {1..80}; do
    grep -q "$1" "$OUT" 2>/dev/null && return 0
    sleep 0.5
  done
  echo "!! timeout waiting for '$1' (see $OUT)"; return 1
}

wait_for "== Page 0" && sleep 0.5
xcrun simctl io "$UDID" screenshot "$SHOT" >/dev/null 2>&1
if (( PUSH )); then
  wait_for "== Page 1" && sleep 1
  xcrun simctl io "$UDID" screenshot "$SHOT_PUSH" >/dev/null 2>&1
fi
xcrun simctl terminate "$UDID" com.example.titleviewrepro >/dev/null 2>&1 || true

echo ">> dump: $OUT"
# Crop with the bar (status bar + title) for a quick comparison
sips --cropToHeightWidth 260 1170 --cropOffset 100 0 "$SHOT" --out "$PWD/bin/$1-bar.png" >/dev/null 2>&1 || true
echo ">> screenshot: $SHOT (bar crop: bin/$1-bar.png)"
