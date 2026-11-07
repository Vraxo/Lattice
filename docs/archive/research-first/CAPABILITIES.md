# Capability matrix and known limitations

**Status:** implementation snapshot for M10.4 (2026-11-02). This describes the checked-in code, not the product vision. “Supported” means implemented and tested within the narrow scope stated here; it does not imply production readiness. “Partial” means a component exists but has a material scope or integration gap. “Unsupported” means no production capability is implemented. Test names refer to files under `Lattice.Core.Tests/` unless a path is given explicitly.

## Availability and execution model

| Capability | Status | Evidence and limits |
|---|---|---|
| CLI demo composition | **Partial** | `Lattice.Cli/Program.cs` manually registers only `CalculatorTool` and creates an `AgentLoop` with `ReadOnlyOnly`. The CLI accepts controlled statements; it does not register the workspace tools, C# or Python adapters, or the natural-language question-answering path. This registration boundary is a manual limitation visible in the composition root. |
| Tool registry, discovery, validation, and permission gate | **Supported (component scope)** | `ToolRegistryTests`, `ToolCatalogTests`, `CapabilityDiscoveryTests`, `ToolArgumentValidatorTests`, `ToolExecutorTests`, and `ToolPermissionPolicyTests`. Hosts explicitly choose which implementations to register. `ToolExecutor` validates arguments and checks permissions before invocation; it does **not** perform general semantic validation of tool outputs. |
| Agent loop and resource bounds | **Partial** | `AgentLoopTests`, `AgentLoopDiscoveryTests`, `AgentRunContextTests`, `ToolExecutionLimitsTests`, `ToolTimeoutTests`, and `CancellationTests`. `AgentLoop.Run` handles one controlled statement or one structured `CapabilityRequest` per call. A host must call it again to run another turn; this is not an autonomous, general-purpose multi-step planner. Timeout/cancellation for synchronous `ITool.Execute` implementations is cooperative: a tool that ignores its cancellation token cannot safely be force-stopped through the current interface. |
| Untrusted tool-output handling | **Supported (permission boundary)** | `UntrustedToolOutputTests` and `ToolExecutorTests`. Tool output is retained as result data and cannot grant permission or trigger a tool call on its own; explicit invocations still pass through the executor’s policy gate. This is not a general semantic validator or sanitizer for arbitrary output text. |
| Controlled statement language | **Supported (v1.0 subset)** | `ControlledLanguageCorpusTests`, `ControlledLanguageParserTests`, and `ControlledStatementInterpreterTests`; grammar and exclusions are in [CONTROLLED_LANGUAGE.md](CONTROLLED_LANGUAGE.md). One statement per line; fixed `fact`, `goal`, `constraint`, and `action` forms. It is not general English or full ACE. |
| Natural-language intent matching and clarification | **Supported (fixed-pattern scope)** | `IntentPatternCatalogTests`, `IntentPatternTests`, `RequestInterpreterTests`, and `ClarificationGeneratorTests`; patterns are in `config/nlp/patterns.json`. Matching is whitespace-token/prefix based, with a small fixed catalog and at most one final slot; no fuzzy matching, stemming, learning, or general language understanding. |
| Natural-language workspace answers | **Partial** | `WorkspaceQuestionAnswererTests` and `EvidenceAnswerTests`. The answerer handles only `find-symbol` and `inspect-path`, using injected search/read/list tools and evidence references. Other matched intents are unsupported by this answerer; the CLI does not invoke it. This is not general question answering or summarization. |
| Session state, facts, and rule derivation | **Partial** | `SessionTests`, `FactTests`, `ObservationTests`, `DerivationTests`, and `RuleEvaluatorTests`. Typed state and simple deterministic rule evaluation exist; there is no general theorem prover or complete world model. Rule evaluation and package retrieval are library APIs, not automatically composed into the CLI loop. |

## Tool and adapter implementations

The tool rows below enumerate the repository’s production `ITool` implementations (excluding test doubles); the adjacent C# inspection/edit rows cover public library APIs included because they are roadmap capabilities. Registration is explicit; apart from the calculator in the CLI, the tools are not enabled by default.

| Capability | Status | Evidence and limits |
|---|---|---|
| `calculator` | **Supported** | `CalculatorToolTests`. Adds two typed integers; no general expression evaluator. This is the only tool registered by the demo CLI. |
| `files.search` | **Supported (bounded)** | `FileSearchToolTests`. Case-insensitive content search under the configured workspace root; default exclusions include `.git`, `.vs`, `artifacts`, `bin`, and `obj`. Files over 262,144 bytes and unreadable/invalid-UTF-8 files are skipped; results cap at 100 matches and displayed lines at 200 characters. It searches content, not filenames. |
| `files.read` | **Supported (bounded)** | `ReadFileToolTests` and `WorkspaceEncodingTests`. Reads files within the configured workspace, up to 262,144 bytes. Reads are BOM-aware: tests directly verify UTF-8 BOM and UTF-16LE BOM decoding; .NET's reader also recognizes other supported Unicode BOMs, but those read cases are not individually asserted here. Without a recognized BOM, content must be strict UTF-8, so invalid byte sequences fail rather than being silently replaced. This is not arbitrary encoding detection. |
| `files.list` | **Supported (bounded output)** | `ListDirectoryToolTests`. Lists one directory inside the configured workspace and displays at most 1,000 sorted entries. |
| `files.write` | **Supported (guarded, narrow patch)** | `WriteFileToolTests`, `WorkspaceEncodingTests`, and `WorkspaceFileAccessEncodingTests`. Applies one unique literal text patch, returns a diff, and uses the shared backup/write path. It is a `LocalWrite` operation and requires a granting policy. It is not an arbitrary rewrite engine. |
| `markdown.edit` | **Supported (narrow section replacement)** | `MarkdownEditToolTests`, `MarkdownDocumentHeadingTests`, `MarkdownDocumentSectionTests`, and `WorkspaceEncodingTests`. Replaces the content under one uniquely identified heading, requires the inspected section hash to match, preserves the heading, and is policy-gated as `LocalWrite`. It does not implement full Markdown parsing or semantic prose rewriting. |
| `command.run` | **Supported (allowlisted only)** | `GuardedCommandToolTests`, `SystemProcessRunnerTests`, and `ToolPermissionPolicyTests`. A host supplies the executable/arguments allowlist; callers provide only an allowed command name. It is `CommandExecution`, requires a granting policy, and returns captured output with truncation. It is not an unrestricted shell. |
| `csharp.syntax` | **Supported (source-text syntax diagnostics)** | `CSharpSyntaxToolTests` and `CSharpSyntaxInspectorTests`. Parses supplied C# source text and reports syntax diagnostics. It does not read project files itself or build a project. The adapter is not registered by the CLI. |
| C# semantic inspection | **Partial (optional library API)** | `CSharpSemanticInspectorTests` and `CSharpSemanticInspectorMultiFileTests`. Can inspect declarations and semantic diagnostics against platform or explicitly supplied metadata references, with caps of 100 diagnostics and 200 declarations. Project files, MSBuild, NuGet resolution, and project-context loading are out of scope. Not a registered `ITool` or CLI feature. |
| C# syntax-aware edit | **Partial (one transformation)** | `CSharpSyntaxEditorTests`. The optional library API adds `sealed` to one uniquely named class and validates the result. It is not a general refactoring/editor and is not registered as a tool or CLI feature. |
| `python.syntax` | **Partial (optional process adapter)** | `Lattice.Core.Tests/PythonSyntaxToolTests.cs`. Uses a host-configured Python 3 interpreter’s standard-library AST parser; submitted source is parsed, never executed. The adapter requires an installed/configured interpreter and `CommandExecution` permission, defaults to a 5-second timeout, and caps source at 8,192 UTF-8 bytes. Tests use a fake process runner, so they do not establish that Python is installed on a host. Not registered by the CLI. |

## Knowledge, retrieval, and edit-intent APIs

| Capability | Status | Evidence and limits |
|---|---|---|
| Versioned knowledge-package parsing and validation | **Supported (schema scope)** | `KnowledgePackageParserTests`, `KnowledgePackageTests`, and `SchemaVersionTests`. Packages are treated as data, not executable code. This does not implement a marketplace, automatic package discovery, or full ontology. |
| Concept/alias lookup and lexical retrieval | **Supported (lexical scope)** | `KnowledgeLookupTests`, `ConceptLookupTests`, `LexicalRetrievalTests`, and `RetrievalBenchmarkTests`. Retrieval is lexical and benchmarked; it is not embedding-based production retrieval. |
| Declarative rule evaluation | **Supported (simple exact-match rules)** | `RuleEvaluatorTests` and `DerivationTests`. Rules match explicit subject/predicate/value patterns and record derivations. This is not broad inference or a general theorem prover. |
| Embedding-based retrieval and related-concept graph traversal | **Unsupported in production** | M9.3 was evaluation-only: `docs/RETRIEVAL_BENCHMARK.md` records the evaluation and explicitly does not approve a production provider, dependency, or graph traversal. No production embedding integration or graph traversal is present. |
| Edit instructions and prose transformations | **Partial** | `EditInstructionTests` and `docs/M8_DESIGN.md`. `ReplaceSection` is marked executable; `Shorten` and `ChangeTone` can be represented but are unsupported and refused by `RequireExecutable`. Representation of an instruction does not mean it was carried out. |

## Exact Markdown and encoding boundaries

The Markdown editor recognizes **ATX headings with at most three leading spaces and optional closing hashes; fenced code with at least three backticks or tildes**. A closing fence must use the same marker and be at least as long as its opener; both headings and fences allow no more than three leading spaces. **Setext headings, tab indentation, inline formatting, and list nesting are unsupported.** Four leading spaces are treated as indented code, not as a heading or fence; backticks in a backtick-fence info string are unsupported. Fences are used to avoid treating heading-like lines inside code blocks as headings; this is a small line-oriented subset, not CommonMark. See `MarkdownDocumentHeadingTests` and `MarkdownDocumentSectionTests`.

Reads are permissive **for BOM-marked Unicode encodings recognized by the .NET reader**; without a recognized BOM, reads require valid UTF-8. Edits are stricter: the shared write boundary accepts only strict UTF-8 without a BOM. UTF-8-with-BOM, UTF-16, UTF-32, invalid, or otherwise unsupported encodings are refused rather than transcoded. Tests: `ReadFileToolTests`, `FileEncodingDetectorTests`, `WorkspaceFileAccessEncodingTests`, and `WorkspaceEncodingTests`.

## Roadmap capabilities not implemented

These are aspirations in `docs/PRD.md` or `docs/ROADMAP.md`, not current supported features:

| Capability | Status | Explicit limitation |
|---|---|---|
| General-purpose planning, arbitrary natural-language requests, and autonomous multi-step task completion | **Unsupported** | The implemented loop is one call/turn at a time, language interpretation is bounded, and the CLI accepts controlled statements. No general-purpose planner or open-ended language model is present. See `AgentLoopTests`, `RequestInterpreterTests`, and the CLI composition noted above. |
| Web search, HTTP/API, databases, MCP, clock, and arbitrary external tools | **Unsupported** | These appear as future/target examples in the PRD; no corresponding production `ITool` implementations are checked into this repository. Hosts may add tools through the generic contracts, but that extensibility is not an implementation of those tools. |
| Broad language ecosystem support (for example Razor, CSS, XML, or arbitrary project analysis) | **Unsupported** | Current language adapters are the bounded C# APIs and optional Python syntax adapter described above. |
| General conversational fluency, unrestricted summarization, semantic shortening/tone changes, and arbitrary refactoring | **Unsupported** | The project has bounded response templates and the single C# edit described above; it has no generative response engine or general semantic editing implementation. |

## Test/build note

This is a manually maintained capability inventory, grounded in the listed tests and explicitly labeled code/roadmap limitations. Passing tests establish only the tested cases, not full conformance to the product vision. The M10.4 build/test gate must be recorded separately after running the repository’s .NET build and full test suite.
