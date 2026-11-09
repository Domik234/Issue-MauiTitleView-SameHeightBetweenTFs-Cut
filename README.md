# TitleView repro: .NET 9 vs .NET 10 on iOS 26+/27

Minimal .NET MAUI app with a custom `NavigationPage.TitleView`, multi-targeting `net9.0-ios` and `net10.0-ios`.
The same code renders correctly on .NET 9 and loses the top of the title on .NET 10 (iOS 26.5 and 27; iOS 18.6 and 18.7 are not affected).

## Repository layout

* [`example/`](example) the MAUI project (`TitleViewRepro.csproj`) and `run.sh`
* [`results/`](results) native layout dumps and screenshots from the runs described below, one folder per device:
  `simulator-iPhone14-iOS18.6/`, `simulator-iPhone14-iOS26.5/`, `simulator-iPhone14-iOS27.0/`, `iPhone16e/` (iOS 27.2), `iPhoneXS/` (iOS 18.7.10)
* [`ISSUE.md`](ISSUE.md) draft of the issue for the MAUI repository

## Symptom

On iOS 26.5 / 27 and .NET 10 the TitleView in the navigation bar is moved up by 5 pt and its top is clipped by the bar
(`UINavigationBar.clipsToBounds = true`). Diacritics above capital letters disappear. The same code on .NET 9 renders correctly.
Crop of the status bar and navigation bar per device (full screenshots and dumps are in the folders under [`results/`](results)):

### iPhone 14 simulator, iOS 26.5 (bar 54 pt) — affected

| net9.0-ios (MAUI 9.0.120) | net10.0-ios (MAUI 10.0.120) |
|---|---|
| ![net9 iOS 26.5](results/simulator-iPhone14-iOS26.5/net9-bar.png) | ![net10 iOS 26.5](results/simulator-iPhone14-iOS26.5/net10-bar.png) |

Dumps: [`results/simulator-iPhone14-iOS26.5/`](results/simulator-iPhone14-iOS26.5) (`net9-dump.txt`, `net10-dump.txt`).

### iPhone 14 simulator, iOS 27.0 (bar 54 pt) — affected

| net9.0-ios (MAUI 9.0.120) | net10.0-ios (MAUI 10.0.120) |
|---|---|
| ![net9](results/simulator-iPhone14-iOS27.0/net9-bar.png) | ![net10](results/simulator-iPhone14-iOS27.0/net10-bar.png) |

Dumps: [`results/simulator-iPhone14-iOS27.0/`](results/simulator-iPhone14-iOS27.0) (`net9-dump.txt`, `net10-dump.txt`).

### iPhone 16e, iOS 27.2 (physical device, bar 54 pt) — affected

| net9.0-ios (MAUI 9.0.120) | net10.0-ios (MAUI 10.0.120) |
|---|---|
| ![net9 iPhone 16e](results/iPhone16e/net9-bar.png) | ![net10 iPhone 16e](results/iPhone16e/net10-bar.png) |

Dumps: [`results/iPhone16e/`](results/iPhone16e).

### iPhone XS, iOS 18.7.10 (physical device, bar 44 pt) — not affected

| net9.0-ios (MAUI 9.0.120) | net10.0-ios (MAUI 10.0.120) |
|---|---|
| ![net9 iPhone XS](results/iPhoneXS/net9-bar.png) | ![net10 iPhone XS](results/iPhoneXS/net10-bar.png) |

Dumps: [`results/iPhoneXS/`](results/iPhoneXS). Here the bar is 44 pt, the container is `(0,0 343×44)` on both target frameworks and the
title wrapper has no offset, so .NET 9 and .NET 10 render the same. (The status bar shows 9:41 because the screenshots were taken over the USB screen capture.)

### iPhone 14 simulator, iOS 18.6 (bar 44 pt) — not affected

| net9.0-ios (MAUI 9.0.120) | net10.0-ios (MAUI 10.0.120) |
|---|---|
| ![net9 iOS 18.6](results/simulator-iPhone14-iOS18.6/net9-bar.png) | ![net10 iOS 18.6](results/simulator-iPhone14-iOS18.6/net10-bar.png) |

Dumps: [`results/simulator-iPhone14-iOS18.6/`](results/simulator-iPhone14-iOS18.6). Same as on the iPhone XS: bar 44 pt, container `(0,0 358×44)` on both
target frameworks. The iOS 18.6 runtime was installed with `xcodebuild -downloadPlatform iOS -buildVersion 18.6` (Xcode 26.5).

## What differs (from the dumps)

On iOS 26.5 and 27 the navigation bar is 54 pt high in all runs (`sizeThatFits` = 54). The numbers below are identical on the iOS 26.5 and 27.0
simulators (iPhone 14) and on the iPhone 16e with iOS 27.2. On the iPhone XS with iOS 18.7.10 and on the iOS 18.6 simulator the bar is 44 pt and both target frameworks give the same result
(container `(0,0 343×44)` on the XS, `(0,0 358×44)` on the simulator), which is consistent with the cause described below: the container follows the bar height, which only differs from the title area on iOS 26+ (not tested on iOS 26.0 to 26.4 and 27.1).

| | .NET 9 | .NET 10 |
|---|---|---|
| `NavigationRenderer.Container` frame | `(0,0 358×44)` | `(0,0 358×54)` |
| system title wrapper frame | `(16,0 358×44)` | `(16,-5 358×54)` |

On .NET 10 the container is sized to the navigation bar height instead of the height UIKit gives the title wrapper.
The wrapper then appears to be centred in a 44 pt slot (centre at y = 22; inferred from the dump, the slot itself is not
visible in the view hierarchy), so 5 pt end up above the bar, where `clipsToBounds` cuts them off.

In MAUI sources (`NavigationRenderer.cs`, class `Container`) this corresponds to:

* 9.0.90 / 9.0.120: `ToolbarHeight` returns `Superview.Bounds.Height` (fallback 44/32).
* 10.0.x: on iOS 26+ the container is created with `navigationBar.Frame` (`CreateTitleViewContainer`) and
  `ToolbarHeight` returns that cached `_navigationBarHeight` first. `Frame` setter and `SizeThatFits` then force it.

MAUI 10.0.120 (which contains the fix for dotnet/maui#39155) behaves the same.

The `clipsToBounds = true` on the bar is set in `NoLineNavigationPage` (hides the line below the bar);
without it the 5 pt overflow is not clipped, the title is only shifted up.

## Run

Requires Xcode 26.5 (for `net9.0-ios`) and Xcode 27.1 (for `net10.0-ios`), because each iOS workload checks the Xcode version.

```sh
cd example
./run.sh net9 --push     # build, run on the simulator, print the layout dump, save screenshots to example/bin/
./run.sh net10 --push
SIM_RUNTIME=iOS-26-5 ./run.sh net10 --push   # the same on the iOS 26.5 simulator (created on first run)
```

Paths to Xcode and the simulator can be changed through environment variables, see the header of `run.sh`.
A simulator (`TitleViewRepro iPhone-14 (iOS-27-0)`, or the one for `SIM_RUNTIME`) is created on first run; delete it with `xcrun simctl delete <udid>`.
Physical device (the device needs a development provisioning profile for `com.example.titleviewrepro`, e.g. a wildcard one):
build with the Xcode matching the TF and `-p:RuntimeIdentifier=ios-arm64 -p:CodesignKey=... -p:CodesignProvision=...`, then install and
launch with `xcrun devicectl device install app` / `device process launch --console --environment-variables
'{"TITLEVIEW_AUTODUMP":"1","TITLEVIEW_AUTOPUSH":"1"}'` (`devicectl` from Xcode 27.1 is needed for an iOS 27.2 device). `run.sh` does not do this.
MAUI version per target framework: `-p:MauiVersionNet9=...` / `-p:MauiVersionNet10=...` (or `MAUI_NET9` / `MAUI_NET10` for `run.sh`).

Without the script: open the app, the buttons change the text, the secondary text and `HeightRequest`,
push another page and print the native layout (`Dump native layout`). The same component is placed in the page content for comparison.

## Notes

* Using the MAUI 9 package with the `net10.0-ios` target framework is not possible (the app crashes at startup:
  `MauiCALayerAutosizeToSuperLayerBehavior` native class not loaded).
* `net9.0-ios` is built against the iOS 26.5 SDK, `net10.0-ios` against 27.1. A `net10.0-ios` build against
  the 26.x SDK was not possible here (the workload requires exactly Xcode 26.0 for it).
* The fonts are Nunito Sans (SIL Open Font License).
