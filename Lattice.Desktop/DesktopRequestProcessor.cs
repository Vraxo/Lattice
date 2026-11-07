using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class DesktopRequestProcessor(RequestInterpreter? interpreter = null)
{
    private const string InspectPatternId = "inspect.path";
    private readonly RequestInterpreter _interpreter = interpreter ?? CreateDefaultInterpreter();

    public static DesktopRequestProcessor Default { get; } = new();

    public DesktopTurnEntry Execute(ConnectedWorkspace? workspace, string rawRequest)
    {
        string requestText = rawRequest?.Trim() ?? string.Empty;
        if (workspace is null)
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InvalidWorkspace,
                "No workspace is connected. Connect an existing local folder before running a request.",
                WorkspacePath: null);
        }

        RequestInterpretation interpretation = _interpreter.Interpret(requestText);
        return interpretation.Kind switch
        {
            IntentMatchKind.Ambiguous => BuildAmbiguousEntry(workspace, requestText, interpretation),
            IntentMatchKind.MissingValue => BuildMissingValueEntry(workspace, requestText, interpretation),
            IntentMatchKind.Unknown => BuildUnsupportedEntry(
                workspace,
                requestText,
                "Unsupported request. Use 'inspect <relative-path>'."),
            IntentMatchKind.Matched => ExecuteMatched(workspace, requestText, interpretation),
            _ => BuildUnsupportedEntry(
                workspace,
                requestText,
                "Unsupported request. Use 'inspect <relative-path>'."),
        };
    }

    private static DesktopTurnEntry ExecuteMatched(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        IntentMatch match = interpretation.Match!;
        if (match.PatternId == InspectPatternId && match.Slot is not null)
        {
            return ExecuteInspect(workspace, requestText, match.Slot.Value);
        }

        return BuildUnsupportedEntry(
            workspace,
            requestText,
            "Unsupported request form. Use 'inspect <relative-path>'.");
    }

    private static DesktopTurnEntry ExecuteInspect(
        ConnectedWorkspace workspace,
        string requestText,
        string rawRelativePath)
    {
        string relativePath = rawRelativePath.Trim();
        if (!workspace.Registry.TryResolve(workspace.Toolset.Read, out ITool? readTool))
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.UnexpectedError,
                "The workspace file-read tool is not registered.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath);
        }

        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(relativePath)),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(readTool, arguments, workspace.Policy);
        if (invocation.Result.IsSuccess)
        {
            string contents = invocation.Result.Output!.AsString();
            int lines = contents.Length == 0 ? 0 : contents.Split('\n').Length;
            string lineLabel = lines == 1 ? "1 line" : $"{lines} lines";
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.FileInspected,
                $"Inspected '{relativePath}' ({lineLabel}).",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                FileContents: contents,
                LineCount: lines);
        }

        Error error = invocation.Result.Error!;
        return error.Code switch
        {
            FileToolErrorCodes.PathNotFound => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.MissingFile,
                $"File '{relativePath}' was not found in the connected workspace.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            FileToolErrorCodes.PathOutsideRoot => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"Rejected '{relativePath}': path must be relative and stay inside the connected workspace root.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            FileToolErrorCodes.ReparsePointNotAllowed => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"Rejected '{relativePath}': symbolic links and reparse points are blocked by the static workspace check.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            FileToolErrorCodes.NotAFile => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is a directory, not a file. Specify a relative file path with 'inspect <relative-path>'.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            FileToolErrorCodes.FileTooLarge => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' exceeds the {ReadFileTool.MaxBytes:N0}-byte inspection limit.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            FileToolErrorCodes.EncodingInvalid => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is not valid UTF-8 text and cannot be inspected.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
            _ => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"Could not inspect '{relativePath}': {error.Message}",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code),
        };
    }

    private static DesktopTurnEntry BuildAmbiguousEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        ClarificationRequest? clarification = ClarificationGenerator.TryCreate(interpretation);
        string detail = clarification?.Question ?? "More than one interpretation matched.";
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.AmbiguousInput,
            $"Ambiguous request ({detail}) Use 'inspect <relative-path>'.",
            workspace.Root.FullPath);
    }

    private static DesktopTurnEntry BuildMissingValueEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        string message = interpretation.Intent == RequestIntentKind.InspectPath
            ? "Missing file path. Usage: inspect <relative-path>."
            : "Incomplete request. Usage: inspect <relative-path>.";
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.UnsupportedInput,
            message,
            workspace.Root.FullPath);
    }

    private static DesktopTurnEntry BuildUnsupportedEntry(
        ConnectedWorkspace workspace,
        string requestText,
        string message)
    {
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.UnsupportedInput,
            message,
            workspace.Root.FullPath);
    }

    private static RequestInterpreter CreateDefaultInterpreter()
    {
        string candidate = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException(
                $"Intent pattern configuration was not found at '{candidate}'.",
                candidate);
        }

        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(candidate));
        if (!parsed.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Intent pattern configuration failed to parse: {parsed.Error!.Message}");
        }

        return new RequestInterpreter(parsed.Value);
    }
}
