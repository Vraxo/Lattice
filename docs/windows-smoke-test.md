# Windows Desktop Smoke Test (Desktop 0.2)

This checklist covers the genuinely manual Windows GUI verification steps for `Lattice.Desktop` (`PhotinoX.Blazor` + WebView2) that headless CI cannot assert on its own.

## Prerequisites

- Windows 10 or Windows 11 with the Microsoft Edge WebView2 Runtime installed.
- .NET 10 SDK (`10.0.x`).
- A local checkout of the `Lattice` repository.

## Launch

From PowerShell or Windows Terminal in the repository root:

```powershell
dotnet run --project Lattice.Desktop -c Release
```

## Manual Verification Steps

- [ ] **Window opens cleanly as a chat interface:** A native desktop window titled `Lattice` opens without WebView2 console errors or blank-page flashes. Verify the compact workspace header bar at the top, the conversational empty state in the center, and the multiline message composer (`Send`) at the bottom — with no command-console `Run` button or syntax-teaching prompts.
- [ ] **Invalid workspace rejection:** Enter a non-existent folder path (for example `C:\does-not-exist-lattice-smoke`) and click **Connect**. Confirm an inline validation banner appears and the active workspace remains `Not connected`.
- [ ] **Connect the `Lattice` repository:** Enter the path to your local `Lattice` repository checkout and click **Connect**. Confirm the workspace badge updates to `<full-path> (Read-only)`.
- [ ] **Composer keyboard behavior (`Shift+Enter` vs `Enter`):** Focus the message composer, type a line, press `Shift+Enter`, and confirm a newline is inserted without sending. Replace the draft with `Show me Lattice/WorkspaceRoot.cs.` and press `Enter`; confirm the message sends, the composer clears on acceptance, and focus returns to the composer.
- [ ] **Natural file viewing:** Verify the user message (`You`) and assistant reply (`Lattice`) appear as distinct chat turns, with `Here is 'Lattice/WorkspaceRoot.cs' (... lines).` and a scrollable read-only viewer displaying `Lattice/WorkspaceRoot.cs`. Try `What's in README.md?` as a second file view.
- [ ] **Natural source-text search:** Send `Where is WorkspaceRoot used in this project?` and `Can you find where WorkspaceQuestionAnswerer is used throughout this project?`. Confirm each assistant reply reports actual match and file counts (for example `I found N matching occurrences of 'WorkspaceRoot' across M files.`), displays relative `path:line` citations with matching snippets, and includes the concise note explaining that matches are literal source-text matches rather than semantic references.
- [ ] **Ambiguous multi-file follow-up:** Immediately after a multi-file search such as `Where is WorkspaceRoot used in this project?`, send `Show me that file.`. Confirm Lattice asks which file you would like to open (listing candidate relative paths) and does **not** guess or open the first file.
- [ ] **Unambiguous single-file follow-up:** Connect to the `<repo>\Lattice` subfolder (or search for an identifier that appears in only one file in your connected folder, such as `Where does EnumerateSearchableFiles appear?` inside `<repo>\Lattice`), verify the search reports `in 1 file`, and then send `Show me that file.` (or `Open it.`). Confirm Lattice opens that single file inline.
- [ ] **Workspace switch clears follow-up context:** Re-connect the top-level `<repo>` folder in the workspace bar and immediately send `Show me that file.`. Confirm Lattice asks which file you want to open rather than reusing the previous workspace's file reference.
- [ ] **Plain-language error, unknown, and ambiguity handling:** Test:
  - `show README.md` (asks a targeted clarification question without raw pattern IDs)
  - `What's in Lattice/DoesNotExist.cs?` (plain-language missing-file reply, no raw `files.path.*` error codes)
  - `Show me ../outside.txt.` (plain-language workspace-confinement rejection)
  - `Rewrite README.md to add badges.` (brief explanation of what Lattice can currently help with, without `Unsupported request` / `Supported forms` console phrasing)
  Confirm all earlier user and assistant turns remain visible in chronological order.
- [ ] **Resize, scroll, keyboard navigation, and close:** Resize the window between normal and narrow widths, confirm long code lines scroll horizontally inside `.file-contents` without breaking the window layout, check keyboard `Tab` focus rings across controls and scrollable panes, and close the window cleanly.
