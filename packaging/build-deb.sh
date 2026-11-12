#!/usr/bin/env bash
# Builds siquester_<version>_amd64.deb into ./deb
set -euo pipefail
cd "$(dirname "$0")/.."
VERSION=$(grep -oP '(?<=<Version>)[^<]+' src/SIQuester/SIQuester.Avalonia/SIQuester.Avalonia.csproj)
P=deb/siquester_${VERSION}_amd64
rm -rf "$P" publish && mkdir -p "$P/DEBIAN" "$P/opt/siquester" "$P/usr/bin" "$P/usr/share/applications" "$P/usr/share/icons/hicolor/256x256/apps"
dotnet publish src/SIQuester/SIQuester.Avalonia -c Release -r linux-x64 --self-contained -o publish
cp -r publish/. "$P/opt/siquester/"
chmod +x "$P/opt/siquester/SIQuester"
ln -s /opt/siquester/SIQuester "$P/usr/bin/siquester"
cp assets/siquester-256.png "$P/usr/share/icons/hicolor/256x256/apps/siquester.png"
cat > "$P/usr/share/applications/siquester.desktop" <<D
[Desktop Entry]
Type=Application
Name=SIQuester
Comment=Редактор пакетов вопросов для SIGame
Exec=/opt/siquester/SIQuester %f
Icon=siquester
Terminal=false
Categories=Game;Utility;
MimeType=application/x-siq;
D
cat > "$P/DEBIAN/control" <<C
Package: siquester
Version: $VERSION
Section: games
Priority: optional
Architecture: amd64
Installed-Size: $(du -sk "$P/opt" | cut -f1)
Depends: libc6, libstdc++6, libgcc-s1, zlib1g, libssl3 | libssl3t64, libx11-6, libxext6, libxcursor1, libxi6, libxrandr2, libice6, libsm6, libfontconfig1, libfreetype6, libgtk-3-0 | libgtk-3-0t64
Recommends: libgl1, xdg-desktop-portal, pkexec | policykit-1
Maintainer: siquester-linux <noreply@users.noreply.github.com>
Description: SIQuester - question package editor for SIGame (Avalonia port)
 Unofficial Linux port of the SIGame question editor. Opens and edits .siq packages.
C
fakeroot dpkg-deb --build "$P" "deb/siquester_${VERSION}_amd64.deb"
echo "Built deb/siquester_${VERSION}_amd64.deb"
