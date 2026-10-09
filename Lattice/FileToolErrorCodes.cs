namespace Lattice.Core;
public static class FileToolErrorCodes
{
    public const string PathOutsideRoot = "files.path.outside-root";
    public const string PathNotFound = "files.path.not-found";
    public const string NotADirectory = "files.path.not-a-directory";
    public const string NotAFile = "files.path.not-a-file";
    public const string FileTooLarge = "files.file.too-large";
    public const string EncodingInvalid = "files.file.encoding-invalid";
    public const string ReadFailed = "files.file.read-failed";
}