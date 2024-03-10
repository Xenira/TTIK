#!/bin/bash
set -e

MOD_DLL="ttik"
GAME_PATH="/c/Program Files (x86)/Steam/steamapps/common/Techtonica"

# Build the project
echo "Building the project..."
cd plugin
dotnet build
cd ..

# Copy the mod dll to the mods folder
echo "Copying the mod dll to the mods folder..."
rm -rf "$GAME_PATH/BepInEx/plugins/TTIK"
mkdir -p "$GAME_PATH/BepInEx/plugins/TTIK/assets"
cp ./plugin/bin/Debug/netstandard2.1/$MOD_DLL.* "$GAME_PATH/BepInEx/plugins/TTIK"
cp ./unity/AssetBundles/StandaloneWindows/* "$GAME_PATH/BepInEx/plugins/TTIK/assets"
