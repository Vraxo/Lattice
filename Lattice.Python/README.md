# Lattice.Python

Optional Python syntax-inspection capability for Lattice. This project references `Lattice.Core` and has no NuGet package dependencies. It is not referenced by Core or the CLI.

## Capability

`PythonSyntaxTool` implements the generic `ITool` contract with id `python.syntax`, aliases `python-ast` and `python-syntax`, and discovery tags `python`, `syntax`, `parse`, and `inspect`. It uses the configured Python 3 interpreter's standard-library `ast.parse` to inspect source text. Submitted source is base64-encoded as process data and is never passed to `exec` or `eval`.

The adapter starts a process, so its descriptor is marked `CommandExecution`. `ToolExecutor` must grant that side-effect class before invoking it. The tool applies isolated interpreter flags (`-I -S`), a five-second default timeout, and an 8,192-byte UTF-8 source limit. The executable and working directory are configured by the host; no interpreter is downloaded or installed by the adapter.

## Optional registration

Register the adapter through the existing registry; omit it to disable the capability. No Core orchestration changes or CLI registration are required:

```csharp
ToolRegistry registry = new([new PythonSyntaxTool("python3")]);
```

A host can select a platform-specific Python executable and pass a custom `IProcessRunner`, working directory, or timeout through the constructor. Normal tests use a fake process runner and do not require Python to be installed.
