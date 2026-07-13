using System;
using System.Collections.Generic;
using System.Text;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Holds all named symbols collected from a successfully parsed Topsy Turvy programme.
/// </summary>
/// <remarks>
/// <para>
/// Symbols refer to all variables, functions and function parameters, each of which is represented by a <see cref="SymbolInfo"/>
/// object.  The symbol table is built by walking the AST.  Definition positions are taken directly from the <see cref="BWHazel.TopsyTurvy.Ast.Node.Span"/>
/// on each node which is populated by the parser at parse time.
/// </para>
/// <para>
/// It should be noted that there is a limitation regarding function parameters.  Currently the symbol table is a flat dictionary
/// of all symbols, therefore, if a function parameter has the same name as a variable or another function parameter, the symbol
/// table will only contain one entry for that name.  This is not a problem for the interpreter, which uses the AST to resolve
/// scopes, but analysis tools will incorrectly use the single symbol table entry for all references to that name.
/// </para>
/// </remarks>
public class SymbolTable
{
    private readonly Dictionary<string, SymbolInfo> symbols;

    /// <summary>
    /// Initialises a new instance of the <see cref="SymbolTable"/> class.
    /// </summary>
    /// <param name="symbols">The symbols.</param>
    private SymbolTable(Dictionary<string, SymbolInfo> symbols)
    {
        this.symbols = symbols;
    }

    /// <summary>
    /// Builds a <see cref="SymbolTable"/>.
    /// </summary>
    /// <param name="program">The root node of the parsed programme.</param>
    /// <param name="originalSource">The original (unprocessed) source text, used to find documentation comments.</param>
    /// <remarks>
    /// This walks the AST.  Definition positions are taken from each node <see cref="BWHazel.TopsyTurvy.Ast.Node.Span"/>.
    /// </remarks>
    /// <returns>A populated <see cref="SymbolTable"/>.</returns>
    public static SymbolTable Build(ProgramNode program, string originalSource)
    {
        Dictionary<string, SymbolInfo> collectedSymbols = new(StringComparer.OrdinalIgnoreCase);
        string[] sourceLines = originalSource.Split('\n');

        collectedSymbols[Keywords.SpecialNames.TheProps] = new SymbolInfo()
        {
            Name = Keywords.SpecialNames.TheProps,
            Kind = SymbolKind.Variable,
            IsConstant = true,
            TypeDisplayName = $"CONSERVATIVE {Keywords.TypeNames.LittleListOf} {Keywords.TypeNames.Yarn}"
        };

        CollectFromStatements(program.Statements, collectedSymbols, sourceLines);
        return new SymbolTable(collectedSymbols);
    }

    /// <summary>
    /// Attempts to retrieve a symbol by name (case-insensitive).
    /// </summary>
    /// <remarks>
    /// <code>
    /// bool symbolFound = symbolTable.TryGetSymbol("LovesickMaidens", out SymbolInfo? info);
    /// if (symbolFound)
    /// {
    ///   Console.WriteLine($"Symbol {info.Name} is a {info.Kind} defined at line {info.DefinitionLine}");
    /// }
    /// </code>
    /// </remarks>
    /// <param name="name">The symbol name to look up.</param>
    /// <param name="info">When this method returns, contains the <see cref="SymbolInfo"/> if found.</param>
    /// <returns><c>true</c> if the symbol was found, otherwise <c>false</c>.</returns>
    public bool TryGetSymbol(string name, out SymbolInfo? info) =>
        this.symbols.TryGetValue(name, out info);

    /// <summary>
    /// Returns all symbols in the table.
    /// </summary>
    /// <remarks>
    /// <code>
    /// foreach (SymbolInfo symbol in symbolTable.AllSymbols())
    /// {
    ///     Console.WriteLine($"Symbol {symbol.Name} is a {symbol.Kind} defined at line {symbol.DefinitionLine}");
    /// }
    /// </code>
    /// </remarks>
    /// <returns>All <see cref="SymbolInfo"/> entries.</returns>
    public IEnumerable<SymbolInfo> AllSymbols() => this.symbols.Values;

    /// <summary>
    /// Extracts the identifier word from source at the given 0-indexed line and column.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <param name="line">The 0-indexed line number.</param>
    /// <param name="column">The 0-indexed character column.</param>
    /// <remarks>
    /// <para>
    /// Extraction of a word follows the process below, which returns early if any check fails:
    /// * The source code is split into lines and any Windows-specific carriage return characters are removed.
    /// * Check that the character at the specified line and column is a valid identifier character.
    /// * Search left and right from the position to find the full word, ensuring it is a valid identifier.
    /// </para>
    /// <code>
    /// string symbol = SymbolTable.ExtractWordAt(source, 10, 15);
    /// if (symbol != null)
    /// {
    ///     Console.WriteLine($"Found symbol: {symbol}");
    /// }
    /// </code>
    /// </remarks>
    /// <returns>
    /// The identifier word at the position, or <c>null</c> if no identifier is present there.
    /// </returns>
    public static string? ExtractWordAt(string source, int line, int column)
    {
        // Splitting on '\n' handles all newlines regardless of platform.
        string[] lines = source.Split('\n');
        if (line < 0 || line >= lines.Length)
        {
            return null;
        }

        // Trimming on a '\r' character ensures that it does not corrupt the column-based substring.
        string lineText = lines[line].TrimEnd('\r');
        if (column < 0 || column >= lineText.Length)
        {
            return null;
        }

        if (!SourceAnalyser.IsIdentifierChar(lineText[column]))
        {
            return null;
        }

        int wordStartColumn = column;
        while (wordStartColumn > 0 && SourceAnalyser.IsIdentifierChar(lineText[wordStartColumn - 1]))
        {
            wordStartColumn--;
        }

        int wordEndColumn = column;
        while (wordEndColumn < lineText.Length - 1 && SourceAnalyser.IsIdentifierChar(lineText[wordEndColumn + 1]))
        {
            wordEndColumn++;
        }

        if (!char.IsLetter(lineText[wordStartColumn]))
        {
            return null;
        }

        return lineText.Substring(wordStartColumn, wordEndColumn - wordStartColumn + 1);
    }

    /// <summary>
    /// Walks the given statements recursively to collect symbol information.
    /// </summary>
    /// <param name="statements">The statements to process.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void CollectFromStatements(IReadOnlyList<Statement> statements, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        foreach (Statement statement in statements)
        {
            CollectFromStatement(statement, collectedSymbols, sourceLines);
        }
    }

    /// <summary>
    /// Walks the given statement recursively to collect symbol information.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void CollectFromStatement(Statement statement, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        switch (statement)
        {
            case PrincipalBlockNode principals:
                foreach (Statement declaration in principals.Declarations)
                {
                    if (declaration is DeclarationNode scalarDeclaration)
                    {
                        AddVariable(scalarDeclaration, collectedSymbols, sourceLines);
                    }
                    else if (declaration is ArrayDeclarationNode arrayDecl)
                    {
                        AddArrayVariable(arrayDecl, collectedSymbols, sourceLines);
                    }
                }

                break;
            case DeclarationNode declaration:
                AddVariable(declaration, collectedSymbols, sourceLines);
                break;
            case ArrayDeclarationNode arrayDeclaration:
                AddArrayVariable(arrayDeclaration, collectedSymbols, sourceLines);
                break;
            case FunctionDefinitionNode function:
                AddFunction(function, collectedSymbols, sourceLines);
                break;
            case ConditionalNode conditional:
                CollectFromStatements(conditional.TrueBlock, collectedSymbols, sourceLines);
                foreach (ElseIfBranch elseIf in conditional.ElseIfs)
                {
                    CollectFromStatements(elseIf.Block, collectedSymbols, sourceLines);
                }

                CollectFromStatements(conditional.ElseBlock, collectedSymbols, sourceLines);
                break;
            case LoopNode loop:
                CollectFromStatements(loop.Body, collectedSymbols, sourceLines);
                break;
            case SwitchNode switchNode:
                foreach (SwitchCase switchCase in switchNode.Cases)
                {
                    CollectFromStatements(switchCase.Block, collectedSymbols, sourceLines);
                }

                CollectFromStatements(switchNode.DefaultBlock, collectedSymbols, sourceLines);
                break;
            case TryCatchNode tryCatch:
                CollectFromStatements(tryCatch.SuccessBlock, collectedSymbols, sourceLines);
                CollectFromStatements(tryCatch.ExceptionBlock, collectedSymbols, sourceLines);
                break;
            case NamespaceDeclarationNode namespaceDeclaration:
                AddNamespace(namespaceDeclaration, collectedSymbols, sourceLines);
                break;
        }
    }

    /// <summary>
    /// Adds a variable to the collected symbol information.
    /// </summary>
    /// <remarks>
    /// If the variable name already exists, it is not added again.
    /// </remarks>
    /// <param name="declaration">The declaration node representing the variable.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddVariable(DeclarationNode declaration, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        if (collectedSymbols.ContainsKey(declaration.Name))
        {
            return;
        }

        collectedSymbols[declaration.Name] = new SymbolInfo()
        {
            Name = declaration.Name,
            Kind = SymbolKind.Variable,
            IsConstant = declaration.IsConstant,
            TypeDisplayName = LiteralTypeToDisplayName(declaration.Type),
            DeclaredType = declaration.Type,
            DefinitionLine = declaration.NameSpan.Start.Line,
            DefinitionColumn = declaration.NameSpan.Start.Column,
            Documentation = FindDocumentationComment(sourceLines, declaration.NameSpan.Start.Line)
        };
    }

    /// <summary>
    /// Adds an array variable to the collected symbol information.
    /// </summary>
    /// <remarks>
    /// If the variable name already exists, it is not added again.
    /// </remarks>
    /// <param name="declaration">The array declaration node representing the variable.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddArrayVariable(ArrayDeclarationNode declaration, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        if (collectedSymbols.ContainsKey(declaration.Name))
        {
            return;
        }

        collectedSymbols[declaration.Name] = new SymbolInfo()
        {
            Name = declaration.Name,
            Kind = SymbolKind.Variable,
            IsConstant = declaration.IsConstant,
            TypeDisplayName = $"{Keywords.TypeNames.LittleListOf} {(declaration.Size.HasValue
                ? $"{declaration.Size.Value} "
                : "")}{LiteralTypeToDisplayName(declaration.ElementType)}",
            DefinitionLine = declaration.NameSpan.Start.Line,
            DefinitionColumn = declaration.NameSpan.Start.Column,
            Documentation = FindDocumentationComment(sourceLines, declaration.NameSpan.Start.Line)
        };
    }

    /// <summary>
    /// Adds a function and its parameters to the collected symbol information.
    /// </summary>
    /// <param name="function">The function definition node representing the function.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddFunction(FunctionDefinitionNode function, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        if (!collectedSymbols.ContainsKey(function.Name))
        {
            collectedSymbols[function.Name] = new SymbolInfo()
            {
                Name = function.Name,
                Kind = SymbolKind.Function,
                DeclaredType = function.ReturnType,
                TypedParameters = function.Parameters,
                DefinitionLine = function.NameSpan.Start.Line,
                DefinitionColumn = function.NameSpan.Start.Column,
                Documentation = FindDocumentationComment(sourceLines, function.NameSpan.Start.Line)
            };
        }

        foreach (TypedParameter parameter in function.Parameters)
        {
            if (!collectedSymbols.ContainsKey(parameter.Name))
            {
                collectedSymbols[parameter.Name] = new SymbolInfo()
                {
                    Name = parameter.Name,
                    Kind = SymbolKind.Parameter,
                    DeclaredType = parameter.Type,
                    TypeDisplayName = LiteralTypeToDisplayName(parameter.Type),
                    DefinitionLine = parameter.Span.Start.Line,
                    DefinitionColumn = parameter.Span.Start.Column
                };
            }
        }

        CollectFromStatements(function.Body, collectedSymbols, sourceLines);
    }

    /// <summary>
    /// Adds a file namespace declaration to the collected symbol information.
    /// </summary>
    /// <param name="namespaceDeclaration">The namespace declaration node.</param>
    /// <param name="collectedSymbols">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddNamespace(NamespaceDeclarationNode namespaceDeclaration, Dictionary<string, SymbolInfo> collectedSymbols, string[] sourceLines)
    {
        string name = string.Join('*', namespaceDeclaration.Path);
        if (collectedSymbols.ContainsKey(name))
        {
            return;
        }

        collectedSymbols[name] = new SymbolInfo()
        {
            Name = name,
            Kind = SymbolKind.Namespace,
            DefinitionLine = namespaceDeclaration.Span.Start.Line,
            DefinitionColumn = namespaceDeclaration.Span.Start.Column,
            Documentation = FindDocumentationComment(sourceLines, namespaceDeclaration.Span.Start.Line)
        };
    }

    /// <summary>
    /// Finds and parses a documentation comment block immediately preceding a symbol definition line.
    /// </summary>
    /// <param name="sourceLines">The original source lines.</param>
    /// <param name="definitionLine">The 1-indexed line number of the symbol definition.</param>
    /// <remarks>
    /// Scans backwards from the line above the declaration, skipping blank lines, looking for an
    /// <c>END OF ASIDE.)</c> marker.  If found, continues scanning to find the matching
    /// <c>(ASIDE, AT SOME LENGTH:</c> opener and extracts the block content for parsing.  Any intervening
    /// non-blank, non-comment line breaks the association and <c>null</c> is returned.
    /// </remarks>
    /// <returns>
    /// A <see cref="DocumentationComment"/> if a documentation block with recognised tags is found
    /// immediately before the definition, otherwise <c>null</c>.
    /// </returns>
    private static DocumentationComment? FindDocumentationComment(string[] sourceLines, int definitionLine)
    {
        if (definitionLine <= 1)
        {
            return null;
        }

        // Scan backwards from the line immediately above the declaration, skipping blank lines.
        int searchLine = definitionLine - 2;
        while (searchLine >= 0 && string.IsNullOrWhiteSpace(sourceLines[searchLine]))
        {
            searchLine--;
        }

        if (searchLine < 0)
        {
            return null;
        }

        // The first non-blank line above the declaration must be the `END OF ASIDE.)` closer.
        string closerLine = sourceLines[searchLine].Trim();
        if (!closerLine.EndsWith("END OF ASIDE.)", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int closerLineIndex = searchLine;

        // Scan further backwards to find the matching (ASIDE, AT SOME LENGTH: opener.
        int openerLineIndex = -1;
        for (int i = closerLineIndex - 1; i >= 0; i--)
        {
            string candidateLine = sourceLines[i].Trim();
            if (candidateLine.StartsWith("(ASIDE, AT SOME LENGTH:", StringComparison.OrdinalIgnoreCase))
            {
                openerLineIndex = i;
                break;
            }
        }

        if (openerLineIndex < 0)
        {
            return null;
        }

        // Extract the content between the opener and closer lines.
        StringBuilder content = new();
        for (int i = openerLineIndex + 1; i < closerLineIndex; i++)
        {
            content.AppendLine(sourceLines[i]);
        }

        return DocumentationCommentParser.Parse(content.ToString());
    }

    /// <summary>
    /// Converts a <see cref="LiteralType"/> to a user-friendly display name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The user-friendly display name.</returns>
    public static string LiteralTypeToDisplayName(LiteralType type) => type switch
    {
        LiteralType.Integer => Keywords.TypeNames.Peer,
        LiteralType.Long => Keywords.TypeNames.Chancellor,
        LiteralType.Short => Keywords.TypeNames.Pirate,
        LiteralType.SignedByte => Keywords.TypeNames.SausageRoll,
        LiteralType.UnsignedInteger => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Peer}",
        LiteralType.UnsignedLong => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Chancellor}",
        LiteralType.UnsignedShort => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Pirate}",
        LiteralType.Byte => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.SausageRoll}",
        LiteralType.Double => Keywords.TypeNames.Fathom,
        LiteralType.Single => Keywords.TypeNames.Foot,
        LiteralType.String => Keywords.TypeNames.Yarn,
        LiteralType.Char => Keywords.TypeNames.Stitch,
        LiteralType.Boolean => Keywords.TypeNames.Decree,
        LiteralType.Null => Keywords.TypeNames.Naught,
        LiteralType.Array => Keywords.TypeNames.LittleListOf,
        _ => "unknown"
    };
}
