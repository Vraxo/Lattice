using System.Text;
namespace Lattice.Core;
public sealed class ReadFileTool : ITool
{
    public const int MaxBytes = 262_144;
    private const string PathParameter = "path";
    private readonly WorkspaceRoot _root;
    public ReadFileTool(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }
    public static ToolId Id { get; } = new("files.read");
    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Reads a UTF-8 text file inside the workspace.",
        "The file contents as text.",
        ToolSideEffect.ReadOnly,
        new[] { new ToolParameter(PathParameter, ToolParameterType.String) });
    public ToolResult Execute(ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(PathParameter, out var value) || value.Type != ToolParameterType.String)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"Argument '{PathParameter}' must be a string."));
        }
        var requested = value.AsString();
        if (!_root.TryResolve(requested, out var fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }
        if (Directory.Exists(fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.NotAFile,
                $"Path '{requested}' is a directory."));
        }
        if (!File.Exists(fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathNotFound,
                $"File '{requested}' was not found."));
        }
        var info = new FileInfo(fullPath);
        if (info.Length > MaxBytes)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.FileTooLarge,
                $"File '{requested}' is {info.Length} bytes; the limit is {MaxBytes}."));
        }
        try
        {
            // Reject invalid UTF-8 instead of silently substituting replacement characters.
            var text = File.ReadAllText(fullPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            return ToolResult.Success(ArgumentValue.FromString(text));
        }
        catch (DecoderFallbackException)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.EncodingInvalid,
                $"File '{requested}' is not valid UTF-8."));
        }
        catch (IOException exception)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"File '{requested}' could not be read: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"File '{requested}' could not be read: {exception.Message}"));
        }
    }
}