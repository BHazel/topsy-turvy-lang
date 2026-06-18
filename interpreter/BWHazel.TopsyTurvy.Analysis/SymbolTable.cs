using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Holds all named symbols collected from a successfully parsed Topsy Turvy programme.
/// </summary>
/// <remarks>
/// <para>
/// Symbols refer to all variables, functions and function parameters, each of which is represented by a <see cref="SymbolInfo"/>
/// object.  The symbol table is built by walking the AST and scanning the original source text to recover definition positions.
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
    /// <param name="originalSource">The original (unprocessed) source text.</param>
    /// <remarks>
    /// This walks the AST and scans the original source line-by-line to recover definition positions.
    /// </remarks>
    /// <returns>A populated <see cref="SymbolTable"/>.</returns>
    public static SymbolTable Build(ProgramNode program, string originalSource)
    {
        Dictionary<string, SymbolInfo> collectedSymbols = new(StringComparer.OrdinalIgnoreCase);
        string[] sourceLines = originalSource.Split('\n');

        collectedSymbols[Keywords.SpecialNames.JustSo] = new SymbolInfo()
        {
            Name = Keywords.SpecialNames.JustSo,
            Kind = SymbolKind.Variable,
            TypeDisplayName = "implicit variable"
        };

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

        SourceLocation definition = FindDefinitionLine(sourceLines, "PRAY WELCOME", declaration.Name);
        collectedSymbols[declaration.Name] = new SymbolInfo()
        {
            Name = declaration.Name,
            Kind = SymbolKind.Variable,
            IsConstant = declaration.IsConstant,
            TypeDisplayName = LiteralTypeToDisplayName(declaration.Type),
            DefinitionLine = definition.Line,
            DefinitionColumn = definition.Column
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

        SourceLocation definition = FindDefinitionLine(sourceLines, "PRAY WELCOME", declaration.Name);
        collectedSymbols[declaration.Name] = new SymbolInfo()
        {
            Name = declaration.Name,
            Kind = SymbolKind.Variable,
            IsConstant = declaration.IsConstant,
            TypeDisplayName = $"{Keywords.TypeNames.LittleListOf} {(declaration.Size.HasValue
                ? $"{declaration.Size.Value} "
                : "")}{LiteralTypeToDisplayName(declaration.ElementType)}",
            DefinitionLine = definition.Line,
            DefinitionColumn = definition.Column
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
        SourceLocation functionDefinition = FindDefinitionLine(
            sourceLines,
            "IT IS MY DUTY TO PERFORM",
            function.Name);

        if (!collectedSymbols.ContainsKey(function.Name))
        {
            collectedSymbols[function.Name] = new SymbolInfo()
            {
                Name = function.Name,
                Kind = SymbolKind.Function,
                Parameters = function.Parameters,
                DefinitionLine = functionDefinition.Line,
                DefinitionColumn = functionDefinition.Column
            };
        }

        foreach (string parameter in function.Parameters)
        {
            if (!collectedSymbols.ContainsKey(parameter))
            {
                collectedSymbols[parameter] = new SymbolInfo()
                {
                    Name = parameter,
                    Kind = SymbolKind.Parameter,
                    DefinitionLine = functionDefinition.Line,
                    DefinitionColumn = functionDefinition.Column
                };
            }
        }

        CollectFromStatements(function.Body, collectedSymbols, sourceLines);
    }

    /// <summary>
    /// Finds the location of a symbol definition by searching for a keyword followed by the symbol name.
    /// </summary>
    /// <param name="sourceLines">The source lines to search.</param>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="name">The name of the symbol to find.</param>
    /// <returns>The 1-indexed location of the symbol definition, or <c>(0, 0)</c> if not found.</returns>
    private static SourceLocation FindDefinitionLine(string[] sourceLines, string keyword, string name)
    {
        for (int i = 0; i < sourceLines.Length; i++)
        {
            string line = sourceLines[i];
            int keywordIndex = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (keywordIndex < 0)
            {
                continue;
            }

            int searchFromIndex = keywordIndex + keyword.Length;
            int nameIndex = line.IndexOf(name, searchFromIndex, StringComparison.OrdinalIgnoreCase);
            if (nameIndex < 0)
            {
                continue;
            }

            // The characters immediately before and after the name must not be valid identifier characters.
            bool isLeadingCharacterValid = nameIndex == 0 || !SourceAnalyser.IsIdentifierChar(line[nameIndex - 1]);
            bool isTrailingCharacterValid = nameIndex + name.Length >= line.Length
                || !SourceAnalyser.IsIdentifierChar(line[nameIndex + name.Length]);

            if (isLeadingCharacterValid && isTrailingCharacterValid)
            {
                return new(i + 1, nameIndex + 1);
            }
        }

        return new(0, 0);
    }


    /// <summary>
    /// Converts a <see cref="LiteralType"/> to a user-friendly display name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The user-friendly display name.</returns>
    private static string LiteralTypeToDisplayName(LiteralType type) => type switch
    {
        LiteralType.Integer => Keywords.TypeNames.Peer,
        LiteralType.Float => Keywords.TypeNames.Fathom,
        LiteralType.String => Keywords.TypeNames.Yarn,
        LiteralType.Boolean => Keywords.TypeNames.Decree,
        LiteralType.Null => Keywords.TypeNames.Naught,
        LiteralType.Array => Keywords.TypeNames.LittleListOf,
        _ => "unknown"
    };
}
