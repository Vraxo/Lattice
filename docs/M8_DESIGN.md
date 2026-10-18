# M8 — Prose and Structured-Document Editing: Design Notes
**Status:** in progress. Records decisions and invariants for milestone M8. Where this document and
the code disagree, the code wins and this document is corrected.
## 1. Scope of M8
M8 adds useful Markdown editing without assuming every problem is code. It builds on the line and
document models (`LineRange`, `TextDocument`, `TextEditor`, `MarkdownDocument`) and the tools
`files.write` and `markdown.edit`.
## 2. Encoding-preservation invariant (authoritative)
An edit must never silently change a file's encoding or its byte-order mark. When the write path
cannot prove that the original encoding will be preserved, the edit is refused.
Concretely, at the shared file-editing boundary (`WorkspaceFileAccess`):
- **UTF-8 without a byte-order mark** may be edited normally, provided strict UTF-8 validation and
  the existing write safeguards pass.
- **UTF-8 with a byte-order mark** is refused with `EncodingPreservationUnsupported` until the mark
  can be provably preserved.
- **UTF-16 LE / BE** and **recognised UTF-32 encodings** are refused with the same typed error until
  preservation is implemented and verified.
- **An unrecognised or invalid encoding** fails safely with `EncodingInvalid`. It is never assumed
  safe to rewrite as UTF-8.
Safety properties required of a refused edit:
1. Detection is performed from the **original file bytes**, before any destructive write.
2. The refusal happens **before the original is modified** and **before a backup is created or
   replaced**.
3. After refusal the original file is **byte-for-byte unchanged**.
The error for a refused edit is the typed code `EncodingPreservationUnsupported`. A refused edit is
distinct from a stale-content error (`document.stale-content`) and from a Markdown parsing error; it
must not be reported as either.
### Why refuse rather than transcode
Decoding a file into a string is lossy about its original encoding. `File.ReadAllText` with implicit
byte-order-mark detection will happily read a UTF-16 file and a UTF-8 BOM file and hand back text;
writing that text back with a UTF-8 encoder silently changes the file's encoding and drops its mark.
That is a destructive side effect of an ordinary content edit. The conservative initial capability
is safe refusal. Encoding-**preserving** edits are deliberate future work and must arrive with
regression tests before they replace the refusal.
### Reads are permissive; writes are guarded
Reading a marked file into a string is not the same as rewriting it. A read is not rejected merely
because a later edit would be unsafe. The engine may inspect a UTF-8 BOM or UTF-16 file; it will
refuse to edit it. These guarantees are complementary: Lattice can see the file, and Lattice will not
damage it. Rejection is a property of the write path only.
## 3. Where the guard lives
The guard lives in `WorkspaceFileAccess`, the single read/backup/write path shared by `WriteFileTool`
(`files.write`) and `MarkdownEditTool` (`markdown.edit`). Both tools inherit the protection by using
the shared helper; their own tests demonstrate that inheritance rather than assuming it. There is no
per-tool encoding logic.
Encoding detection is a separate, tested decision point: `FileEncodingDetector.Detect` classifies raw
bytes, checking byte-order marks longest and most specific first (UTF-32 LE before UTF-16 LE, because
their marks share the `FF FE` prefix), then falling back to strict UTF-8 validation for unmarked
bytes.
## 4. Known limits and deliberate non-goals
- No encoding-preserving writes yet. A UTF-8 BOM file, a UTF-16 file, and a UTF-32 file are all
  **uneditable** in M8; they remain readable.
- No per-tool encoding configuration.
- No automatic transcoding, ever, as a side effect of a content edit.
- The read tools (`files.read`, `files.search`) are not refactored by this change and keep their
  existing behaviour: they read supported BOM-marked Unicode files and reject invalid UTF-8.
## 5. Acceptance criteria for the encoding substep (M8.4)
- UTF-8 without a BOM edits normally; the result has no BOM.
- UTF-8 with a BOM, UTF-16 LE, UTF-16 BE, UTF-32 LE, and UTF-32 BE are each refused with
  `EncodingPreservationUnsupported`.
- A refused edit leaves the original file byte-for-byte unchanged and neither creates nor replaces a
  backup.
- Both `files.write` and `markdown.edit` exhibit the refusal, demonstrating the shared boundary
  protects both.
- Reads of marked files still succeed; a rejected edit is not confused with a stale-content or
  parsing error.
- `dotnet build` and `dotnet test` pass with a reported, real test count.
## 6. English realization templates (M8.3)
Response text is produced by filling fixed templates (`ResponseTemplates`) with values the engine
observed. This replaces ad-hoc string interpolation at the answer sites.
### Invariant
A realized response states only what its supplied slots establish. A template cannot introduce a
fact that was not passed to it, because every variable part of a response is a named slot and every
slot is filled from an observed value (a tool result, a parsed question, or a derived count).
### Rules
- A template is a fixed string with named slots written `{name}`. Its slot names are discovered from
  the text, not declared separately.
- Realization fails with the typed error `ResponseErrorCodes.MissingSlot` when a slot has no value.
  A placeholder is never emitted; the caller either supplies the slot or the realization is a
  failure.
- At the answer boundary (`WorkspaceQuestionAnswerer`) a missing slot is a programming error — a
  call site and its template disagree — so it throws rather than returning a half-formed answer.
- Slot values are inserted verbatim and are not re-scanned for slots, so a value that itself
  contains braces is not expanded.
- Counts are pluralized by `EnglishPlural`: exactly one uses the singular form, everything else the
  plural. This fixes the previously ungrammatical `entr(y/ies)` output.
- Templates live in code, not configuration. They are fixed system output, not user-extensible
  coverage, so a parser and schema would not be justified. (Contrast `config/nlp/patterns.json`,
  which is data because adding coverage there is the point.)
### Tests
- `tests/corpus/responses/v1.json` holds one entry per template with its expected realized text,
  plus negative `missingSlotCases`. A wording change that alters meaning must change the fixture
  deliberately.
- `ResponseRealizationTests` drives the corpus, asserts every catalog template is covered, that no
  realized text leaves a `{`/`}`, that a brace-containing value is not re-expanded, and that
  `EnglishPlural` behaves for 0/1/2 and rejects negatives.
- `WorkspaceQuestionAnswererTests` adds end-to-end assertions that answered text is fully realized
  and grammatically singular where expected.
### Non-goals
- No unrestricted prose generation, and no model of tone, style, or paraphrase.
- No template selection by scoring; the caller chooses the template that matches its evidence.
- No localization or message catalogs.