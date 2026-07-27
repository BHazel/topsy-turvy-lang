using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Resolves signature help for an in-progress <c>SUMMON</c> call.
/// </summary>
/// <remarks>
/// In Topsy Turvy, function calls take the form <c>SUMMON functionName WITH arg1 AND arg2 IF YOU PLEASE.</c>. This
/// scans the text before the cursor to identify the function being called, the arguments typed so far, and which
/// parameter is currently active, entirely from source text and a <see cref="SymbolTable"/>; neither the host
/// request/response types are referenced here, so both can call this directly and map the result into their own shape.
/// </remarks>
public static class SignatureHelpBuilder
{
    /// <summary>
    /// Resolves signature help at the given cursor position.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <param name="line">The 0-indexed line number of the cursor.</param>
    /// <param name="character">The 0-indexed character offset of the cursor on that line.</param>
    /// <param name="symbolTable">The symbol table to resolve the called function name against.</param>
    /// <remarks>
    /// * The text before the cursor on the current line is scanned for a <c>SUMMON</c> keyword. If not found, or the call is already complete (the closing <c>IF YOU PLEASE.</c> is present), <c>null</c> is returned.
    /// * The function name is extracted as the first token following <c>SUMMON</c> and looked up in the symbol table. If not found or not a function, <c>null</c> is returned.
    /// * If no <c> WITH</c> has been typed yet, or the argument list begins with <c>NOTHING</c> (a zero-argument call), <c>null</c> is returned.
    /// * The argument index currently being typed is determined by counting <c>AND</c> separators in the argument text using <see cref="CountAndTokens"/>.
    /// * Every overload declared under the function name is fetched from <paramref name="symbolTable"/> and turned into its own <see cref="SignatureCandidate"/>.
    /// * <see cref="FindActiveSignature"/> picks the first overload, in declaration order, whose parameter count still has room for the argument index above, defaulting to the first overload if none do; the active parameter index is then clamped to the last parameter index of that overload.
    /// </remarks>
    /// <returns>The resolved <see cref="SignatureHelpResult"/>, or <c>null</c> if no in-progress function call is detected at the cursor position.</returns>
    public static SignatureHelpResult? Build(string source, int line, int character, SymbolTable symbolTable)
    {
        string[] lines = source.Split('\n');
        if (line < 0 || line >= lines.Length)
        {
            return null;
        }

        string lineText = lines[line];
        int safeCharacter = Math.Min(character, lineText.Length);
        string textBeforeCursor = lineText[..safeCharacter];

        int summonIndex = textBeforeCursor.LastIndexOf("SUMMON", StringComparison.OrdinalIgnoreCase);
        if (summonIndex < 0)
        {
            return null;
        }

        string afterSummonText = textBeforeCursor[(summonIndex + "SUMMON".Length)..].TrimStart();
        if (afterSummonText.IndexOf("IF YOU PLEASE.", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return null;
        }

        string[] tokens = afterSummonText.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }

        string functionName = tokens[0];
        if (!symbolTable.TryGetSymbol(functionName, out SymbolInfo? info) || info is null
            || info.Kind != SymbolKind.Function)
        {
            return null;
        }

        int withIndex = afterSummonText.IndexOf(" WITH", StringComparison.OrdinalIgnoreCase);
        if (withIndex < 0)
        {
            return null;
        }

        string afterWithText = afterSummonText[(withIndex + " WITH".Length)..];
        if (afterWithText.TrimStart().StartsWith("NOTHING", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int calledArgumentIndex = CountAndTokens(afterWithText);

        IReadOnlyList<SymbolInfo> overloads = symbolTable.GetFunctionOverloads(functionName) is { Count: > 0 } candidates
            ? candidates
            : [info];

        List<SignatureCandidate> signatures = [.. overloads.Select(BuildSignatureCandidate)];
        int activeSignature = FindActiveSignature(overloads, calledArgumentIndex);
        int activeParameter = overloads[activeSignature].TypedParameters is { Count: > 0 } activeParameters
            ? Math.Min(calledArgumentIndex, activeParameters.Count - 1)
            : 0;

        return new(signatures, activeSignature, activeParameter);
    }

    /// <summary>
    /// Counts whole-word joining tokens (case-insensitive) in the specified text.
    /// </summary>
    /// <remarks>
    /// Each <c>AND</c> token separates one function argument from the next so the count corresponds to the
    /// zero-based index of the parameter currently being typed.
    /// </remarks>
    /// <param name="text">The text to scan.</param>
    /// <returns>The number of <c>AND</c> tokens found.</returns>
    private static int CountAndTokens(string text)
    {
        int count = 0;
        foreach (string token in text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("AND", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Builds the signature candidate for a single function overload.
    /// </summary>
    /// <param name="overload">The overload to build a candidate for.</param>
    /// <returns>The built <see cref="SignatureCandidate"/>.</returns>
    /// <remarks>
    /// Each parameter label includes its declared type (<c>"name AS A type"</c>), matching the convention
    /// <see cref="HoverMarkdownBuilder"/> already uses for a function signature: without the type, two overloads
    /// differing only by parameter type render as identical labels.
    /// </remarks>
    private static SignatureCandidate BuildSignatureCandidate(SymbolInfo overload)
    {
        IReadOnlyList<TypedParameter> typedParameters = overload.TypedParameters ?? [];
        List<string> parameterLabels = [.. typedParameters
            .Select(static parameter => $"{parameter.Name} AS A {LiteralTypeNames.ToDisplayName(parameter.Type)}")];

        string returnPart = overload.DeclaredType is LiteralType returnType
            ? $" TO FIND {LiteralTypeNames.ToDisplayName(returnType)}"
            : string.Empty;

        string label = $"{overload.Name}({string.Join(", ", parameterLabels)}){returnPart}";
        return new(label, parameterLabels);
    }

    /// <summary>
    /// Picks which overload should be highlighted as the active signature.
    /// </summary>
    /// <param name="overloads">The candidate overloads, in declaration order.</param>
    /// <param name="calledArgumentIndex">The zero-based index of the argument currently being typed.</param>
    /// <returns>The index, within <paramref name="overloads"/>, of the first overload with room for that argument, or <c>0</c> if none have room.</returns>
    /// <remarks>
    /// Typing is in progress, so the argument types seen so far cannot be resolved with any confidence; picking by
    /// arity alone is enough to steer the pop-up towards a plausible candidate as the user types, without the
    /// heavier by-type resolution the type checker and interpreter perform against a complete, finished call.
    /// </remarks>
    private static int FindActiveSignature(IReadOnlyList<SymbolInfo> overloads, int calledArgumentIndex)
    {
        for (int i = 0; i < overloads.Count; i++)
        {
            int parameterCount = overloads[i].TypedParameters?.Count ?? 0;
            if (parameterCount > calledArgumentIndex)
            {
                return i;
            }
        }

        return 0;
    }
}
