# SIQuester for Linux

Unofficial Linux port of **SIQuester**, the question package editor for SIGame.
The UI is rewritten in [Avalonia](https://avaloniaui.net/); package logic and view models
come from the original [VladimirKhil/SI](https://github.com/VladimirKhil/SI) project (MIT).

## Status (0.2.0)
Works: open/save `.siq`; add, clone, remove, reorder rounds, themes and questions; question types, text/image/audio/video content, answers, authors/sources/comments, tags, undo/redo.
Not yet: export (docx/table/html), import from text/DB, preview and game testing, media storage panel, drag-and-drop, settings persistence.

## Build
```bash
dotnet build src/SIQuester/SIQuester.Avalonia -c Release   # needs .NET 10 SDK
./packaging/build-deb.sh                                    # produces deb/siquester_<ver>_amd64.deb
```

## Updates
The **⟳ Обновить** button checks the latest GitHub release of this repo and installs its `.deb` (via `pkexec apt`).
Publish a release with tag `vX.Y.Z` and attach the `.deb`.

## License
MIT, see `LICENSE` (original copyright VladimirKhil).
