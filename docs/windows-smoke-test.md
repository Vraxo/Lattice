# Windows Desktop Smoke Test (Desktop 0.4)

This checklist covers the genuinely manual Windows GUI verification steps for `Lattice.Desktop` (`PhotinoX.Blazor` + WebView2) that headless CI cannot assert on its own.

## Prerequisites

- Windows 10 or Windows 11 with the Microsoft Edge WebView2 Runtime installed.
- .NET 10 SDK (`10.0.x`).
- A local checkout of the `Lattice` repository.

## Launch

From PowerShell or Windows Terminal in the repository root:

```powershell
dotnet run --project Lattice.Desktop/Lattice.Desktop.csproj --configuration Release
```

## Manual Verification Steps

- [ ] **Window opens with cohesive neutral charcoal/dark-gray styling:** Verify the native `Lattice` desktop window opens cleanly with neutral charcoal backgrounds (`#232323`), slightly lighter dark-gray panels (`#2b2b2b`), darker inset inputs/viewers (`#1c1c1c`), off-white text, neutral gray buttons, and neutral gray focus rings — with **no** blue-black/navy cast or bright blue message bubbles/buttons.
- [ ] **Invalid workspace rejection:** Enter a non-existent folder path (for example `C:\does-not-exist-lattice-smoke`) and click **Connect**. Confirm an inline validation banner appears with readable contrast and the active workspace remains `Not connected`.
- [ ] **Connect the `Lattice` repository:** Enter the path to your local `Lattice` repository checkout and click **Connect**. Confirm the workspace indicator updates to `<full-path>` with a subtle connected indicator dot.
- [ ] **Root-level basename precedence:** Send `What's in README.md?`. Confirm Lattice opens the root `README.md` (not `docs/README.md`) in the inline read-only viewer.
- [ ] **Unique nested basename discovery:** Send `Show me WorkspaceRoot.cs.`. Confirm Lattice locates `Lattice/WorkspaceRoot.cs` automatically without requiring the folder prefix, shows `Here is 'Lattice/WorkspaceRoot.cs' (... lines).`, and displays its contents inline.
- [ ] **Extensionless filename-stem lookup & numbered selection:** Send `Show me program`. Confirm it finds files named `Program.cs` by stem; when there are duplicates, reply with `2` and confirm the second displayed candidate opens. An exact extensionless filename named `program` takes precedence over stem matches.
- [ ] **Duplicate basename clarification & candidate selection (`Show me Program.cs`):**
  1. Send `Show me Program.cs.`.
  2. Confirm Lattice does **not** guess or read any file yet; instead, it lists the matching relative paths (`Lattice.Cli/Program.cs`, `Lattice.Desktop/Program.cs`, and `tests/evaluation/m9_3/baseline/Program.cs`) and asks which one to open.
  3. Reply directly with `Lattice.Desktop/Program.cs` (or `Show me Lattice.Desktop/Program.cs.`).
  4. Confirm Lattice opens `Lattice.Desktop/Program.cs` inline.
- [ ] **Missing basename vs explicit path precedence:**
  - Send `Show me NonExistentWidget.cs.` and confirm Lattice replies that it couldn't find a file with that name anywhere in the connected workspace.
  - Send `Show me NonExistentFolder/Program.cs.` and confirm Lattice reports `'NonExistentFolder/Program.cs' was not found` rather than substituting another folder's `Program.cs`.
- [ ] **Natural source-text search & follow-up context:**
  - Send `Where is WorkspaceRoot used in this project?` and confirm relative `path:line` citations, snippets, and the literal source-text-match note render legibly in the charcoal theme.
  - Send `Show me that file.` immediately after the multi-file search and confirm Lattice asks which file to open rather than guessing.
  - Send `Show me Lattice/WorkspaceRoot.cs.` followed by `Open it.` (or connect `<repo>\Lattice` and ask `Where does EnumerateDirectoryEntriesSafely appear?` followed by `Show me that file.`) and confirm the single unambiguous file opens.
- [ ] **C# type inventory:** Ask `What classes are in this project?` and `What types are in this project?`. Confirm each C# declaration has a kind, signature, relative path, and line; markdown text and `bin/`/`obj/` output must not appear as declarations.
- [ ] **C# definition lookup and follow-up:** Ask `Where is WorkspaceRoot defined?`. Confirm the actual class declaration is shown (not README or comment matches), then ask `Show me that file.` and verify the source file opens when the declaration result points to one file.
- [ ] **C# member inspection:** Ask `Show me the members of WorkspaceRoot.`. Confirm constructors, methods, properties, fields, and other directly declared members are shown with signatures and source locations.
- [ ] **C# syntax-only boundary:** Confirm the result labels itself syntax-based and does not claim to find usages/call sites or prove the project compiles. Test a file with syntax errors and verify any partial result is described honestly.
- [ ] **Composer keyboard behavior, resize, and close:** Verify `Shift+Enter` inserts a newline, `Enter` sends and restores composer focus, keyboard `Tab` focus rings are clearly visible across buttons/inputs/code panes, narrow window widths wrap cleanly without horizontal page overflow, and closing the window exits cleanly.
