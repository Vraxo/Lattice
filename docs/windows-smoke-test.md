# Windows desktop smoke test

Automated CI verifies solution restore, Release build, workspace symlink/reparse-point safety boundaries, and headless bUnit component rendering (`Lattice.Desktop.Tests`), including connecting to the real `Lattice` checkout and exercising `inspect` and `find symbol`. Headless tests do **not** open a native Photino/WebView2 window or verify OS window lifecycle, focus, scrolling, or resize behavior.

Run the manual walkthrough below on a Windows 10/11 machine with the .NET 10 SDK installed before marking the remaining manual GUI launch items complete in [`.agents/todo.md`](../.agents/todo.md).

## Launch commands

From the repository root in PowerShell:

```powershell
dotnet restore Lattice.slnx
dotnet build Lattice.slnx --configuration Release --no-restore
dotnet test Lattice.slnx --configuration Release --no-build
dotnet run --project Lattice.Desktop/Lattice.Desktop.csproj --configuration Release --no-build
```

## Manual checklist

- [ ] **Window launch:** Confirm the native Photino window opens with title `Lattice`, dark theme styling applied (`wwwroot/app.css`), and active workspace indicator showing `Not connected`.
- [ ] **Invalid workspace validation:** Leave the folder path blank or enter a nonexistent path (for example `C:\does-not-exist-lattice`) and click **Connect**; confirm a clear alert message is shown and the indicator remains `Not connected`.
- [ ] **Connect real repository (Phase 1.2):** Enter the full local path to your `Lattice` checkout and click **Connect**; confirm **Active workspace** updates to that resolved path in read-only mode.
- [ ] **Inspect real file (Phase 3.2):** Enter `inspect Lattice/WorkspaceRoot.cs` and click **Run**; confirm the turn card displays `Lattice/WorkspaceRoot.cs`, line count, and scrollable C# source contents.
- [ ] **Find symbol with grounded evidence (Phase 4 / 5.2):** Enter `find symbol WorkspaceQuestionAnswerer` and click **Run**; confirm the UI labels the result as **Source-text search** (not semantic symbol resolution) and lists matching relative file paths, 1-based line numbers (for example `Lattice/WorkspaceQuestionAnswerer.cs:11`), and line snippets while keeping the earlier `inspect` turn visible above/below in `#turn-history`.
- [ ] **Error and empty states:** Run `find symbol NonExistentSymbolXYZ`, `inspect Lattice/MissingFile.cs`, `inspect ../outside.txt`, `show README.md`, and `hello`; confirm distinct `No Matches`, `Missing File`, `Access Rejected`, `Ambiguous Input`, and `Unsupported Input` cards appear and prior history remains intact.
- [ ] **Window lifecycle:** Resize the window narrow and wide, verify keyboard Tab focus across inputs and buttons, and close/relaunch cleanly without errors.
