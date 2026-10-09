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
    public const string QueryEmpty = "files.search.query-empty";
    public const string QueryInvalid = "files.search.query-invalid";
    public const string PatchContextNotFound = "files.patch.context-not-found";
    public const string PatchContextAmbiguous = "files.patch.context-ambiguous";
    public const string WriteFailed = "files.write.failed";
}