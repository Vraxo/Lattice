using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// The fixed set of responses the agent currently realizes. Each template states only what the
/// supplying facts or tool results establish; slots are filled from values the caller observed.
/// </summary>
public static class ResponseTemplates
{
    public static ResponseTemplate FindSymbolAnswered { get; } =
        new("find-symbol.answered", "Found '{symbol}' in {files}, on {lines}.");

    public static ResponseTemplate FindSymbolNotFound { get; } =
        new("find-symbol.not-found", "No occurrences of '{symbol}' were found in the workspace.");

    public static ResponseTemplate SearchFailed { get; } =
        new("search.failed", "The search failed: {errorCode}.");

    public static ResponseTemplate SearchUnavailable { get; } =
        new("search.unavailable", "The search capability is not available.");

    public static ResponseTemplate ReadAnswered { get; } =
        new("read.answered", "Read '{path}' ({lines}).");

    public static ResponseTemplate ReadFailed { get; } =
        new("read.failed", "'{path}' could not be read: {errorCode}.");

    public static ResponseTemplate ReadUnavailable { get; } =
        new("read.unavailable", "The read capability is not available.");

    public static ResponseTemplate DirectoryAnswered { get; } =
        new("directory.answered", "'{path}' is a directory containing {entries}.");

    public static ResponseTemplate DirectoryFailed { get; } =
        new("directory.failed", "'{path}' could not be listed.");

    public static ResponseTemplate UnsupportedInterpretation { get; } =
        new("interpretation.unsupported", "I could not interpret that as a supported workspace question.");

    public static ResponseTemplate UnsupportedIntent { get; } =
        new("intent.unsupported", "I can only search for a symbol or inspect a path so far.");

    public static ImmutableArray<ResponseTemplate> All { get; } =
    [
        FindSymbolAnswered,
        FindSymbolNotFound,
        SearchFailed,
        SearchUnavailable,
        ReadAnswered,
        ReadFailed,
        ReadUnavailable,
        DirectoryAnswered,
        DirectoryFailed,
        UnsupportedInterpretation,
        UnsupportedIntent,
    ];
}