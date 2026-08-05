using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents a named symbol found in a Topsy Turvy source file.
/// </summary>
/// <remarks>
/// <para>
/// An instance of <see cref="SymbolInfo"/> is used to represent a symbol in a Topsy Turvy programme.
/// </para>
/// <para>
/// In the following example:
/// </para>
/// <code>
/// PRAY WELCOME AllLords AS A PEER
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AND Liberals
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// <para>
/// there are 4 symbols in total:
/// * <c>AllLords</c> is a <see cref="SymbolKind"/><c>.Variable</c> with a type display name of <c>PEER</c>.
/// * <c>TotalLords</c> is a <see cref="SymbolKind"/><c>.Function</c> with parameters <c>Conservatives</c> and <c>Liberals</c>.
/// * <c>Conservatives</c> is a <see cref="SymbolKind"/><c>.Parameter</c> for the <c>TotalLords</c> function.
/// * <c>Liberals</c> is also a <see cref="SymbolKind"/><c>.Parameter</c> for the <c>TotalLords</c> function.
/// </para>
/// and would be represented by the following instances of <see cref="SymbolInfo"/>:
/// <code>
/// SymbolInfo allLordsSymbol = new()
/// {
///     Name = "AllLords",
///     Kind = SymbolKind.Variable,
///     TypeDisplayName = "PEER",
///     DefinitionLine = 1,
///     DefinitionColumn = 1
/// };
/// 
/// SymbolInfo totalLordsSymbol = new()
/// {
///     Name = "TotalLords",
///     Kind = SymbolKind.Function,
///     TypedParameters =
///     [
///         new TypedParameter("Conservatives", LiteralType.Integer, new() { /* ... */ }),
///         new TypedParameter("Liberals", LiteralType.Integer, new() { /* ... */ })
///     ],
///     DeclaredType = LiteralType.Integer,
///     DefinitionLine = 2,
///     DefinitionColumn = 1
/// };
///
/// SymbolInfo conservativesSymbol = new()
/// {
///     Name = "Conservatives",
///     Kind = SymbolKind.Parameter,
///     DefinitionLine = 2,
///     DefinitionColumn = 38
/// };
/// 
/// SymbolInfo liberalsSymbol = new()
/// {
///     Name = "Liberals",
///     Kind = SymbolKind.Parameter,
///     DefinitionLine = 2,
///     DefinitionColumn = 52
/// };
/// </code>
/// <para>
/// Any associated documentation comments for a variable or function symbol are parsed by the
/// <see cref="DocumentationCommentParser"/> and stored in the <see cref="Documentation"/> property.
/// Applying documentation comments to the examples above, although it should be noted that this is
/// not an exharustive example of the documentation comment syntax:
/// </para>
/// <code>
/// (ASIDE, AT SOME LENGTH:
///     LEGEND: Holds the numbers.
/// END OF ASIDE.)
/// PRAY WELCOME AllLords AS A PEER
/// 
/// (ASIDE, AT SOME LENGTH:
///     LEGEND: Adds the total number of Lords.
///     ARTICLE Conservatives (PEER): The number of Conservative Lords.
///     ARTICLE Liberals (PEER): The number of Liberal Lords.
///     CONSEQUENCE (PEER): The total number of Lords.
/// END OF ASIDE.)
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AND Liberals
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// the <see cref="SymbolInfo"/> instances associated with the <c>AllLords</c> and <c>TotalLords</c> symbols would have their <see cref="Documentation"/> property populated:
/// <code>
/// SymbolInfo allLordsSymbol = new()
/// {
///     Name = "AllLords",
///     Kind = SymbolKind.Variable,
///     TypeDisplayName = "PEER",
///     DefinitionLine = 1,
///     DefinitionColumn = 1,
///     Documentation = new DocumentationComment
///     {
///         Summary = "Holds the numbers."
///     }
/// };
///
/// SymbolInfo totalLordsSymbol = new()
/// {
///     Name = "TotalLords",
///     Kind = SymbolKind.Function,
///     TypedParameters =
///     [
///         new TypedParameter("Conservatives", LiteralType.Integer, new() { /* ... */ }),
///         new TypedParameter("Liberals", LiteralType.Integer, new() { /* ... */ })
///     ],
///     DeclaredType = LiteralType.Integer,
///     DefinitionLine = 2,
///     DefinitionColumn = 1,
///     Documentation = new DocumentationComment
///     {
///         Summary = "Adds the total number of Lords.",
///         Parameters = new Dictionary&lt;string, (string Type, string Description)&gt;
///         {
///             ["Conservatives"] = ("PEER", "The number of Conservative Lords."),
///             ["Liberals"] = ("PEER", "The number of Liberal Lords.")
///         },
///         ReturnValue = ("PEER", "The total number of Lords.")
///     }
/// };
/// </code>
/// </remarks>
public class SymbolInfo
{
    /// <summary>
    /// Gets or initialises the name of the symbol.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the kind of symbol.
    /// </summary>
    public required SymbolKind Kind { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether this variable is a constant.
    /// </summary>
    /// <remarks>
    /// Only meaningful for <see cref="SymbolKind.Variable"/>.  When <c>true</c>, the variable was declared with the
    /// <c>CONSERVATIVE</c> modifier and cannot be reassigned at runtime.
    /// </remarks>
    public bool IsConstant { get; init; }

    /// <summary>
    /// Gets or initialises the display name of the declared type.
    /// </summary>
    /// <remarks>
    /// Only populated for <see cref="SymbolKind.Variable"/>.
    /// </remarks>
    public string? TypeDisplayName { get; init; }

    /// <summary>
    /// Gets or initialises the declared <see cref="LiteralType"/> of the symbol.
    /// </summary>
    /// <remarks>
    /// Populated for variables from the declaration, parameters from their type annotation and functions
    /// from the <c>TO FIND</c> return type or <c>null</c> for void functions.
    /// </remarks>
    public LiteralType? DeclaredType { get; init; }

    /// <summary>
    /// Gets or initialises the array element type of <see cref="DeclaredType"/>, when it is <see cref="LiteralType.Array"/>.
    /// </summary>
    /// <remarks>
    /// Only populated for a <see cref="SymbolKind.Function"/> symbol whose return type is an array; <c>null</c> otherwise.
    /// A parameter element type is instead read directly from <see cref="TypedParameter.ArrayElementType"/> on
    /// the matching entry in <see cref="TypedParameters"/>.
    /// </remarks>
    public LiteralType? DeclaredArrayElementType { get; init; }

    /// <summary>
    /// Gets or initialises the typed parameter list for a function symbol.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="SymbolKind.Function"/> symbols and <c>null</c> for variables and parameters.
    /// </remarks>
    public IReadOnlyList<TypedParameter>? TypedParameters { get; init; }

    /// <summary>
    /// Gets or initialises the 1-indexed line number of the symbol definition in the original source.
    /// </summary>
    /// <remarks>
    /// Zero indicates the position could not be determined.
    /// </remarks>
    public int DefinitionLine { get; init; }

    /// <summary>
    /// Gets or initialises the 1-indexed column number of the symbol definition in the original source.
    /// </summary>
    /// <remarks>
    /// Zero indicates the position could not be determined.
    /// </remarks>
    public int DefinitionColumn { get; init; }

    /// <summary>
    /// Gets or initialises the parsed documentation comment for this symbol.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no documentation comment block was found immediately before the symbol declaration.
    /// </remarks>
    public DocumentationComment? Documentation { get; init; }
}
