using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Holds all named symbols collected from a successfully parsed Topsy Turvy program.
/// </summary>
/// <remarks>
/// Provides lookup and cursor-position utilities for LSP handlers.
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
    /// <param name="ast">The root node of the parsed program.</param>
    /// <param name="originalSource">The original (unprocessed) source text.</param>
    /// <remarks>
    /// This walks the AST and scans the original source line-by-line to recover definition positions.
    /// </remarks>
    /// <returns>A populated <see cref="SymbolTable"/>.</returns>
    public static SymbolTable Build(ProgramNode ast, string originalSource)
    {
        Dictionary<string, SymbolInfo> collected = new(StringComparer.OrdinalIgnoreCase);
        string[] sourceLines = originalSource.Split('\n');

        collected["JUST SO"] = new SymbolInfo
        {
            Name = "JUST SO",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "implicit accumulator"
        };

        CollectFromStatements(ast.Statements, collected, sourceLines);
        return new SymbolTable(collected);
    }

    /// <summary>
    /// Attempts to retrieve a symbol by name (case-insensitive).
    /// </summary>
    /// <param name="name">The symbol name to look up.</param>
    /// <param name="info">When this method returns, contains the <see cref="SymbolInfo"/> if found.</param>
    /// <returns><c>true</c> if the symbol was found, otherwise <c>false</c>.</returns>
    public bool TryGetSymbol(string name, out SymbolInfo? info) =>
        this.symbols.TryGetValue(name, out info);

    /// <summary>
    /// Returns all symbols in the table.
    /// </summary>
    /// <returns>All <see cref="SymbolInfo"/> entries.</returns>
    public IEnumerable<SymbolInfo> AllSymbols() => this.symbols.Values;

    /// <summary>
    /// Extracts the identifier word from source at the given 0-indexed line and column.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <param name="line">The 0-indexed line number.</param>
    /// <param name="column">The 0-indexed character column.</param>
    /// <returns>
    /// The identifier word at the position, or <c>null</c> if no identifier is present there.
    /// </returns>
    public static string? ExtractWordAt(string source, int line, int column)
    {
        string[] lines = source.Split('\n');
        if (line < 0 || line >= lines.Length)
        {
            return null;
        }

        string lineText = lines[line].TrimEnd('\r');
        if (column < 0 || column >= lineText.Length)
        {
            return null;
        }

        if (!IsIdentifierChar(lineText[column]))
        {
            return null;
        }

        int start = column;
        while (start > 0 && IsIdentifierChar(lineText[start - 1]))
        {
            start--;
        }

        int end = column;
        while (end < lineText.Length - 1 && IsIdentifierChar(lineText[end + 1]))
        {
            end++;
        }

        if (!char.IsLetter(lineText[start]))
        {
            return null;
        }

        return lineText.Substring(start, end - start + 1);
    }

    /// <summary>
    /// Walks the given statements recursively to collect symbol information.
    /// </summary>
    /// <param name="statements">The statements to process.</param>
    /// <param name="collected">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void CollectFromStatements(
        IReadOnlyList<Statement> statements,
        Dictionary<string, SymbolInfo> collected,
        string[] sourceLines)
    {
        foreach (Statement statement in statements)
        {
            CollectFromStatement(statement, collected, sourceLines);
        }
    }

    /// <summary>
    /// Walks the given statement recursively to collect symbol information.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <param name="collected">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void CollectFromStatement(
        Statement statement,
        Dictionary<string, SymbolInfo> collected,
        string[] sourceLines)
    {
        switch (statement)
        {
            case PrincipalBlockNode principals:
                foreach (DeclarationNode decl in principals.Declarations)
                {
                    AddVariable(decl, collected, sourceLines);
                }

                break;
            case DeclarationNode declaration:
                AddVariable(declaration, collected, sourceLines);
                break;
            case FunctionDefinitionNode function:
                AddFunction(function, collected, sourceLines);
                break;
            case ConditionalNode conditional:
                CollectFromStatements(conditional.TrueBlock, collected, sourceLines);
                foreach (ElseIfBranch elseIf in conditional.ElseIfs)
                {
                    CollectFromStatements(elseIf.Block, collected, sourceLines);
                }

                CollectFromStatements(conditional.ElseBlock, collected, sourceLines);
                break;
            case LoopNode loop:
                CollectFromStatements(loop.Body, collected, sourceLines);
                break;
            case SwitchNode switchNode:
                foreach (SwitchCase switchCase in switchNode.Cases)
                {
                    CollectFromStatements(switchCase.Block, collected, sourceLines);
                }

                CollectFromStatements(switchNode.DefaultBlock, collected, sourceLines);
                break;
            case TryCatchNode tryCatch:
                CollectFromStatements(tryCatch.SuccessBlock, collected, sourceLines);
                CollectFromStatements(tryCatch.ExceptionBlock, collected, sourceLines);
                break;
        }
    }

    /// <summary>
    /// Adds a variable to the collected symbol information.
    /// </summary>
    /// <param name="declaration">The declaration node representing the variable.</param>
    /// <param name="collected">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddVariable(
        DeclarationNode declaration,
        Dictionary<string, SymbolInfo> collected,
        string[] sourceLines)
    {
        if (collected.ContainsKey(declaration.Name))
        {
            return;
        }

        (int line, int column) = FindDefinitionLine(sourceLines, "PRAY WELCOME", declaration.Name);
        collected[declaration.Name] = new SymbolInfo
        {
            Name = declaration.Name,
            Kind = SymbolKind.Variable,
            TypeDisplayName = LiteralTypeToDisplayName(declaration.Type),
            DefinitionLine = line,
            DefinitionColumn = column
        };
    }

    /// <summary>
    /// Adds a function and its parameters to the collected symbol information.
    /// </summary>
    /// <param name="function">The function definition node representing the function.</param>
    /// <param name="collected">The dictionary to collect symbol information into.</param>
    /// <param name="sourceLines">The original source lines.</param>
    private static void AddFunction(
        FunctionDefinitionNode function,
        Dictionary<string, SymbolInfo> collected,
        string[] sourceLines)
    {
        (int functionLine, int functionColumn) = FindDefinitionLine(
            sourceLines, "IT IS MY DUTY TO PERFORM", function.Name);

        if (!collected.ContainsKey(function.Name))
        {
            collected[function.Name] = new SymbolInfo
            {
                Name = function.Name,
                Kind = SymbolKind.Function,
                Parameters = function.Parameters,
                DefinitionLine = functionLine,
                DefinitionColumn = functionColumn
            };
        }

        foreach (string parameter in function.Parameters)
        {
            if (!collected.ContainsKey(parameter))
            {
                collected[parameter] = new SymbolInfo
                {
                    Name = parameter,
                    Kind = SymbolKind.Parameter,
                    DefinitionLine = functionLine,
                    DefinitionColumn = functionColumn
                };
            }
        }

        CollectFromStatements(function.Body, collected, sourceLines);
    }

    /// <summary>
    /// Finds the line and column of a symbol definition by searching for a keyword followed by the symbol name.
    /// </summary>
    /// <param name="sourceLines">The source lines to search.</param>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="name">The name of the symbol to find.</param>
    /// <returns>A tuple containing the line and column of the symbol definition.</returns>
    private static (int Line, int Column) FindDefinitionLine(
        string[] sourceLines, string keyword, string name)
    {
        for (int i = 0; i < sourceLines.Length; i++)
        {
            string line = sourceLines[i];
            int keywordIndex = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (keywordIndex < 0)
            {
                continue;
            }

            int searchFrom = keywordIndex + keyword.Length;
            int nameIndex = line.IndexOf(name, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (nameIndex < 0)
            {
                continue;
            }

            bool leadingOk = nameIndex == 0 || !IsIdentifierChar(line[nameIndex - 1]);
            bool trailingOk = nameIndex + name.Length >= line.Length
                || !IsIdentifierChar(line[nameIndex + name.Length]);

            if (leadingOk && trailingOk)
            {
                return (i + 1, nameIndex + 1);
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// Determines if a character is a valid identifier character.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns><c>true</c> if the character is a valid identifier character; otherwise, <c>false</c>.</returns>
    private static bool IsIdentifierChar(char character) =>
        char.IsLetterOrDigit(character) || character == '-' || character == '_';

    /// <summary>
    /// Converts a <see cref="LiteralType"/> to a user-friendly display name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The user-friendly display name.</returns>
    private static string LiteralTypeToDisplayName(LiteralType type) => type switch
    {
        LiteralType.Integer => "PEER",
        LiteralType.Float   => "FATHOM",
        LiteralType.String  => "YARN",
        LiteralType.Boolean => "DECREE",
        LiteralType.Null    => "NAUGHT",
        _                   => "unknown"
    };
}
