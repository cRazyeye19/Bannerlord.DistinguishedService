# Bannerlord.DistinguishedService

A port of the **Distinguished Service** mod for *Mount & Blade II: Bannerlord* to game version **v1.4.7**.

After battles, troops with enough kills can be nominated to become companions (heroes). Thresholds, skill bonuses and other options are in `Settings.xml`.

> **Status:** compiles against 1.4.7 but is lightly tested. Try it on a test save first.

## Install
Copy `dist/Bannerlords.DistinguishedService` into your game's `Modules` folder, then enable it in the launcher after Native, SandBoxCore and Sandbox.

## Build
Requires the .NET SDK. Point `GameBin` at your game's `bin\Win64_Shipping_Client` folder:

    dotnet build -c Release -p:GameBin="D:\Path\To\Bannerlord\bin\Win64_Shipping_Client"

The DLL is built to `bin/Release/net472/`; copy it into `dist/Bannerlords.DistinguishedService/bin/Win64_Shipping_Client/`.

## Changes from the original
- `ImageIdentifier` -> `CharacterImageIdentifier` (`TaleWorlds.Core.ImageIdentifiers`)
- `CultureCode` / `GetCultureCode()` removed -> culture `StringId` is used
- `CultureObject.NotableAndWandererTemplates` removed -> wanderers are looked up by culture + occupation (`DSCompat.cs`)

## Credits and license
Based on the original Distinguished Service mod by its original author. This port was recreated from the published mod binary for personal use. No license has been chosen; check the original author's permissions before redistributing.
