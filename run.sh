#!/bin/zsh
# Sestaví a spustí repro na iOS simulátoru pro zvolený TF, vypíše nativní layout a uloží screenshot.
#
#   ./run.sh net9            .NET 9  (Xcode 26.5)
#   ./run.sh net10           .NET 10 (Xcode 27.1)
#   ./run.sh net10 --push    navíc otevře druhou stránku a vypíše i její layout
#
# Prostředí (volitelné):
#   XCODE_NET9, XCODE_NET10   cesty k Xcode.app pro jednotlivé TF
#   SIM_RUNTIME               runtime simulátoru, výchozí iOS-27-0
#   SIM_NAME                  název simulátoru, výchozí "TitleViewRepro <SIM_TYPE> (<runtime>)"
#   SIM_TYPE                  typ zařízení pro vytvoření simulátoru, výchozí iPhone-14
#   SIM_UDID                  konkrétní simulátor (jinak se použije nebo vytvoří SIM_NAME)
#   MAUI_NET9, MAUI_NET10     verze MAUI (předá se jako MauiVersionNet9 / MauiVersionNet10)
set -euo pipefail
cd "$(dirname "$0")"

case "${1:-}" in
  net9)  TF=net9.0-ios;  XCODE="${XCODE_NET9:-/Applications/Xcode-26.5.0.app}" ;;
  net10) TF=net10.0-ios; XCODE="${XCODE_NET10:-/Applications/Xcode-27.1.0.app}" ;;
  *) echo "použití: ./run.sh net9|net10 [--push]"; exit 1 ;;
esac
export DEVELOPER_DIR="$XCODE/Contents/Developer"
PUSH=0; [[ "${2:-}" == "--push" ]] && PUSH=1

RUNTIME="${SIM_RUNTIME:-iOS-27-0}"
SIM_TYPE="${SIM_TYPE:-iPhone-14}"
SIM_NAME="${SIM_NAME:-TitleViewRepro ${SIM_TYPE} (${RUNTIME})}"
# Běžný iPhone (390×844 @3x) pro daný runtime; zařízení se případně vytvoří. Na tomto stroji má runtime iOS 27.1 jen iPhone Duo (SIM_TYPE=iPhone-Duo).
# Smazání: xcrun simctl delete <udid>
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
  echo ">> vytvářím simulátor '$SIM_NAME'"
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

echo ">> simulátor $UDID"
xcrun simctl boot "$UDID" 2>/dev/null || true
xcrun simctl bootstatus "$UDID" -b >/dev/null
xcrun simctl install "$UDID" "$APP"
xcrun simctl terminate "$UDID" com.example.titleviewrepro 2>/dev/null || true

export SIMCTL_CHILD_TITLEVIEW_AUTODUMP=1
(( PUSH )) && export SIMCTL_CHILD_TITLEVIEW_AUTOPUSH=1
xcrun simctl launch --stdout="$OUT" --stderr="$OUT" "$UDID" com.example.titleviewrepro >/dev/null

# Čeká, až se ve výpisu objeví daný řetězec (první start po instalaci může trvat i několik sekund)
wait_for() {
  for i in {1..80}; do
    grep -q "$1" "$OUT" 2>/dev/null && return 0
    sleep 0.5
  done
  echo "!! timeout při čekání na '$1' (viz $OUT)"; return 1
}

wait_for "== Page 0" && sleep 0.5
xcrun simctl io "$UDID" screenshot "$SHOT" >/dev/null 2>&1
if (( PUSH )); then
  wait_for "== Page 1" && sleep 1
  xcrun simctl io "$UDID" screenshot "$SHOT_PUSH" >/dev/null 2>&1
fi
xcrun simctl terminate "$UDID" com.example.titleviewrepro >/dev/null 2>&1 || true

echo ">> výpis: $OUT"
# Výřez s lištou (status bar + titulek) pro rychlé porovnání
sips --cropToHeightWidth 260 1170 --cropOffset 100 0 "$SHOT" --out "$PWD/bin/$1-bar.png" >/dev/null 2>&1 || true
echo ">> screenshot: $SHOT (výřez lišty: bin/$1-bar.png)"
