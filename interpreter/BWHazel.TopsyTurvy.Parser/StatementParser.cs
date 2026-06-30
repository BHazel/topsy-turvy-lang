using System;
using System.Collections.Generic;
using System.Linq;
using Superpower;
using Superpower.Parsers;
using BWHazel.TopsyTurvy.Ast;
using static BWHazel.TopsyTurvy.Parser.ParserHelpers;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Implements the <see cref="TextParser{T}"/> combinators for Topsy Turvy statement forms.
/// </summary>
/// <remarks>
/// <para>
/// The statement parser is the third layer of the parser which builds on top of the lexer and expression parser to
/// build <see cref="Ast.Statement"/> components.  As with other layers, the statement parser is itself made up of parsers for each
/// type of statement, all of which are built up by smaller building blocks called combinators.  Unlike expressions, statements cannot
/// appear in-line within other statements as arguments.  However, statements that define blocks, such as the
/// <c>SHOULD IT TRANSPIRE THAT</c> conditional, can contain nested statements within their bodies.
/// </para>
/// <para>
/// Please note that for brevity the capture of spans and committed-parse semantics are not documented for each parser individually.  Please see the **Source Spans** and **Committed Parse Semantics** sections below for descriptions of how spans are captured and how block-body parsers handle partial matches.
/// </para>
/// <para>
/// ### Variable Declarations
/// The <c>Declaration</c> parser matches on variable declarations, returning a <see cref="DeclarationNode"/> with the variable name, type, optional mutability modifier and optional initial value:
/// * It first matches the <c>PRAY WELCOME</c> keyword.
/// * It then matches required whitespace followed by an identifier for the variable name using the <see cref="Lexer"/><c>.Identifier</c> parser.
/// * It then matches more required whitespace followed by the <c>AS A</c> keyword.
/// * It then optionally matches a mutability modifier (<c>CONSERVATIVE</c> or <c>LIBERAL</c>), back-tracking if neither is present.
/// * It then matches more required whitespace followed by a type keyword using the <see cref="ExpressionParser"/><c>.TypeKeyword</c> parser.
/// * Finally, it tries to match on an initial value, back-tracking if not matched:
///     * It first matches more required whitespace followed by the <c>BEING</c> keyword.
///     * It then matches more required whitespace followed by an expression for the initial value using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
///     * If no initial value is parsed, a default value of <c>null</c> is used.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// PRAY WELCOME LovesickMaidens AS A CONSERVATIVE PEER BEING 20
/// PRAY WELCOME TotalLords AS A PEER
/// </code>
/// both would return a <see cref="DeclarationNode"/> and for each example:
/// * The first would have the variable name <c>LovesickMaidens</c>, a type of <see cref="LiteralType"/><c>.Integer</c>, <see cref="DeclarationNode.IsConstant"/> set to <c>true</c>, and initial value of an integer literal with value 20.
/// * The second would have the variable name <c>TotalLords</c>, also a type of <see cref="LiteralType"/><c>.Integer</c>, <see cref="DeclarationNode.IsConstant"/> set to <c>false</c> (mutable, the default), and no initial value, thus set to <c>null</c>.
/// </para>
/// <para>
/// ### Assignments
/// The <c>Assignment</c> parser matches on assignment statements, returning an <see cref="AssignmentNode"/> with the target variable
/// name and new value expression:
/// * It first matches an identifier for the target variable name using the <see cref="Lexer"/><c>.Identifier</c> parser.
/// * It then matches required whitespace followed by the <c>IS APPOINTED</c> keyword.
/// * It then matches more required whitespace followed by an expression for the new value using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// 
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// LovesickMaidens IS APPOINTED 15
/// TotalLords IS APPOINTED SUMMON GetTotalLords WITH NOTHING IF YOU PLEASE.
/// </code>
/// both would return an <see cref="AssignmentNode"/> and for each example:
/// * The first would have the target variable name <c>LovesickMaidens</c> and a new value of an integer literal with value 15.
/// * The second would have the target variable name <c>TotalLords</c> and a new value set by the response from a function call to <c>GetTotalLords</c>.
/// </para>
/// <para>
/// ### User I/O
/// 2 parsers are included for interactive user input and output.
/// 
/// #### Print Statements
/// The <c>Print</c> parser matches on output statements, returning a <see cref="PrintNode"/> with the expression to print and whether to suppress the newline:
/// * It first matches the <c>BEHOLD</c> keyword.
/// * It then matches required whitespace followed by an expression for the value to print using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// * It then tries to match on the optional <c>WITHOUT CEREMONY</c> keyword, back-tracking if not matched.
///     * If the keyword is matched, the newline is suppressed.
///     * If the keyword is not matched, the newline is printed.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// BEHOLD "Good morrow, World!  World, good morrow!"
/// BEHOLD TotalLords WITHOUT CEREMONY
/// </code>
/// both would return a <see cref="PrintNode"/> and for each example:
/// * The first would have the string literal <c>Good morrow, World!  World, good morrow!</c> to print and the newline would not be suppressed.
/// * The second would have the variable <c>TotalLords</c> to print and the newline would be suppressed.
/// </para>
/// <para>
/// #### Input Statements
/// The <c>Input</c> parser matches on input statements, returning an <see cref="InputNode"/> with the target variable name to store the input value:
/// * It first matches the <c>PRAY TELL</c> keyword.
/// * It then matches required whitespace followed by an identifier for the target variable name using the <see cref="Lexer"/><c>.Identifier</c> parser.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// PRAY TELL TotalLords
/// </code>
/// the statement would be matched by the <c>Input</c> parser, returning an <see cref="InputNode"/> with the target variable name <c>TotalLords</c>.
/// </para>
/// <para>
/// ### Conditionals
/// 2 parsers work together for matching conditional statements.
/// </para>
/// <para>
/// #### Conditional Body
/// The <c>ConditionalBody</c> parser matches on the body of a conditional statement, the block between the
/// <c>SHOULD IT TRANSPIRE THAT</c> and <c>SO MUCH FOR THAT.</c> keywords to return a <see cref="ConditionalBodyInfo"/>,
/// used by the <c>ConditionalParser</c>:
/// * It first matches on the true block:
///     * It first matches on the true block keyword <c>QUITE SO.</c>.
///     * It then matches on required whitespace then recursively matches on statements in the block.
///         * It back-tracks if a match fails and continues until no more statements are matched.
/// * Secondly, it matches on the else-if blocks, of which there can be zero or more:
///     * It first matches on more required whitespace followed by the else-if block keyword <c>OR, IF NOT,</c>.
///     * It then matches on more required whitespace and recursively matches on an optional condition expression.
///         * It back-tracks if a match fails.
///     * It then matches on required whitespace then recursively matches on statements in the block.
///         * It also back-tracks if a match fails and continues until no more statements are matched.
/// * Finally, it matches on the else block, which is optional:
///     * It first matches on the else block keyword <c>OTHERWISE</c>.
///     * It then matches on required whitespace then recursively matches on statements in the block.
///         * It back-tracks if a match fails and continues until no more statements are matched.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// SHOULD IT TRANSPIRE THAT ALIKE PoemSubject AND "Hollow"
///     QUITE SO.
///         BEHOLD "Bunthorne!"
///     OR, IF NOT, ALIKE PoemSubject AND "Magnet"
///         BEHOLD "Grosvenor!"
///     OTHERWISE,
///         BEHOLD "Not Aesthetic!"
/// SO MUCH FOR THAT.
/// </code>
/// a <see cref="ConditionalBodyInfo"/> is created with:
/// * The <see cref="ConditionalBodyInfo.TrueBlock"/> containing a single item for the <c>BEHOLD</c> statement.
/// * The <see cref="ConditionalBodyInfo.ElseIfs"/> containing:
///     * A single item with the condition <c>ALIKE PoemSubject AND "Magnet"</c> and a single item for the <c>BEHOLD</c> statement.
/// * The <see cref="ConditionalBodyInfo.ElseBlock"/> containing a single item for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// #### Conditional
/// The <c>Conditional</c> parser matches on a complete conditional statement, returning a <see cref="ConditionalNode"/>:
/// * It first matches on the <c>SHOULD IT TRANSPIRE THAT</c> keyword.
/// * It then matches on required whitespace followed by a condition expression.
/// * It then matches on the body of the conditional using the <c>ConditionalBody</c> parser.
/// * It then matches on required whitespace followed by the closing <c>SO MUCH FOR THAT.</c> keyword.
/// </para>
/// <para>
/// In the example above, for the <c>ConditionalBody</c> parser, the complete statement is matched by the <c>Conditional</c> parser,
/// returning a <see cref="ConditionalNode"/> with the condition <c>ALIKE PoemSubject AND "Hollow"</c>, a true block,
/// one else-if branch, and an else block, each populated from the <see cref="ConditionalBodyInfo"/> returned
/// by the <c>ConditionalBody</c> parser.
/// </para>
/// <para>
/// ### Switch
/// 2 parsers work together to match a switch statement.
/// #### Switch Body
/// The <c>SwitchBody</c> parser matches on the case structure of a switch statement, returning a <see cref="SwitchBodyInfo"/>:
/// * It matches on zero or more cases, back-tracking between each:
///     * It first matches on required whitespace followed by the <c>WHEN ACTING AS</c> keyword.
///     * It then matches on required whitespace followed by a literal value.
///     * It then matches on zero or more body statements.
///         * A <c>THAT WILL DO.</c> break is parsed as a <c>Break</c> statement within the body rather than as a separate construct.
/// * It then tries to match on an optional <c>FAILING ALL OF THE ABOVE,</c> default block, back-tracking if not matched, followed by zero or more statements.
/// 
/// Please see the **Switch** section below for an example.
/// </para>
/// <para>
/// #### Switch
/// The <c>Switch</c> parser matches on a complete switch statement, returning a <see cref="SwitchNode"/>:
/// * It first matches on the <c>IN WHICH CAPACITY?</c> keyword.
/// * It then matches on required whitespace followed by an expression to switch on.
/// * It then matches on the case structure using the <c>SwitchBody</c> parser.
/// * It then matches on required whitespace followed by the closing <c>NOTHING COULD BE MORE SATISFACTORY.</c> keyword.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// IN WHICH CAPACITY? office
///     WHEN ACTING AS "Private Secretary"
///         BEHOLD "Don't stint yourself, do it well."
///         THAT WILL DO.
///     WHEN ACTING AS "Chancellor of the Exchequer"
///         BEHOLD "Due economy is observed."
///         THAT WILL DO.
///     FAILING ALL OF THE ABOVE,
///         BEHOLD "No money, no grovel!"
/// NOTHING COULD BE MORE SATISFACTORY.
/// </code>
/// the statement would be matched by the <c>Switch</c> parser, returning a <see cref="SwitchNode"/> with:
/// * <c>Expression</c> set to the variable <c>office</c>.
/// * <see cref="SwitchBodyInfo.Cases"/> containing two items, each with a literal value and a body with a print and a break statement.
/// * <see cref="SwitchBodyInfo.DefaultBlock"/> containing one print statement.
/// </para>
/// <para>
/// ### Loops
/// 5 parsers are included for loop statements.
/// #### Loop Step Parser
/// The <c>LoopStepParser</c> matches on the optional <c>BY &lt;expr&gt;</c> step clause for ascending and descending loops,
/// returning a nullable <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>:
/// * It first matches on required whitespace followed by the <c>BY</c> keyword.
/// * It then matches on required whitespace followed by any expression using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// * If <c>BY</c> is not present the parser back-tracks and returns <c>null</c>.
/// </para>
/// <para>
/// #### Loop Type Parser
/// The <c>LoopTypeParser</c> identifies the type of a loop, returning a <see cref="LoopDefinition"/>.
/// It tries each type in order, back-tracking between them:
/// * <see cref="LoopType" /><c>.Ascending</c> matches an <c>ASCENDING</c> loop with:
///     * A loop variable identifier.
///     * An optional <c>BY &lt;expr&gt;</c> step clause via <c>LoopStepParser</c>, set to <c>null</c> when absent.
///     * The <c>UNTIL</c> keyword.
///     * A condition expression.
/// * <see cref="LoopType" /><c>.Descending</c> matches a <c>DESCENDING</c> loop with:
///     * A loop variable identifier.
///     * An optional <c>BY &lt;expr&gt;</c> step clause via <c>LoopStepParser</c>, set to <c>null</c> when absent.
///     * The <c>UNTIL</c> keyword.
///     * A condition expression.
/// * <see cref="LoopType" /><c>.Whilst</c> matches a <c>WHILST</c> loop with:
///     * A condition expression.
/// * <see cref="LoopType" /><c>.Infinite</c> matches an infinite loop if none of the above are matched.
/// </para>
/// <para>
/// #### Break
/// The <c>Break</c> parser matches on the <c>THAT WILL DO.</c> keyword, returning a <see cref="BreakNode"/> to exit the
/// nearest enclosing loop.
/// </para>
/// <para>
/// #### Continue
/// The <c>Continue</c> parser matches on the <c>ONCE MORE.</c> keyword, returning a <see cref="ContinueNode"/> to skip
/// to the next iteration of the nearest enclosing loop.
/// </para>
/// <para>
/// #### Loop
/// The <c>Loop</c> parser matches on a complete loop statement, returning a <see cref="LoopNode"/>:
/// * It first matches on the <c>BY A LEGAL FICTION</c> keyword.
/// * It then tries to match on an optional label introduced by <c>KNOWN AS</c> followed by an identifier, back-tracking if not matched.
/// * It then matches on the loop type using the <c>LoopTypeParser</c>, which for ascending/descending loops also captures the optional step expression.
/// * It then matches on zero or more body statements.
/// * Finally it matches on required whitespace followed by the closing <c>THE TERM EXPIRES.</c> keyword.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// BY A LEGAL FICTION KNOWN AS HeavyDragoons ASCENDING count UNTIL ALIKE count AND 7
///     BEHOLD "A heavy dragoon!"
/// THE TERM EXPIRES.
///
/// BY A LEGAL FICTION ASCENDING rank BY 3 UNTIL PRE-ADAMITE rank AND 20
///     BEHOLD rank
/// THE TERM EXPIRES.
/// </code>
/// * The first would be matched by the <c>Loop</c> parser, returning a <see cref="LoopNode"/> with:
///     * The label set to <c>HeavyDragoons</c>.
///     * The loop type set to <see cref="LoopType.Ascending"/>.
///     * The loop variable set to <c>count</c>.
///     * The condition set to the expression <c>ALIKE count AND 7</c>.
///     * <see cref="LoopNode.Step"/> set to <c>null</c> as no <c>BY</c> clause is included therefore defaults to 1.
///     * The body containing one print statement.
/// * The second would be matched by the <c>Loop</c> parser, returning a <see cref="LoopNode"/> with:
///     * No label.
///     * The loop type set to <see cref="LoopType.Ascending"/>.
///     * The loop variable set to <c>rank</c>.
///     * The condition set to the expression <c>PRE-ADAMITE rank AND 20</c>.
///     * <see cref="LoopNode.Step"/> set to the integer literal expression <c>3</c>, evaluated each iteration.
///     * The body containing one print statement.
/// </para>
/// <para>
/// ### Guard Clauses
/// The <c>Guard</c> parser matches on a guard clause, returning a <see cref="GuardNode"/>:
/// * It first matches on the <c>YEOMAN</c> keyword.
/// * It then matches on required whitespace followed by a condition expression using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// * It then matches on required whitespace followed by the <c>OTHERWISE,</c> keyword.
/// * It then matches on zero or more body statements for the else block, each back-tracked individually.
/// * Finally it matches on required whitespace followed by the closing <c>UNDER ORDERS.</c> keyword.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// YEOMAN PRE-ADAMITE score AND 0
///     OTHERWISE,
///         A HIDEOUS CURSE ON "Score must be positive"
/// UNDER ORDERS.
/// </code>
/// the statement would be matched by the <c>Guard</c> parser, returning a <see cref="GuardNode"/> with:
/// * The condition set to the expression <c>PRE-ADAMITE score AND 0</c>.
/// * The else block containing one throw statement.
/// </para>
/// <para>
/// ### Exception Handling
/// The <c>TryCatch</c> parser matches on a try-catch block, returning a <see cref="TryCatchNode"/>:
/// * It first matches on the <c>WITH THE GREATEST RESPECT,</c> keyword.
/// * It then matches on required whitespace followed by a single expression for the operation to guard.
/// * It then matches on required whitespace followed by the <c>WITH GRATITUDE</c> keyword to open the success block.
/// * It then matches on zero or more statements for the success block, executed if the guarded expression succeeds.
/// * It then matches on required whitespace followed by the <c>MODIFIED RAPTURE,</c> keyword to open the exception block.
/// * It then matches on required whitespace followed by an identifier for the caught value binding.
/// * It then matches on zero or more statements for the exception block, executed if the guarded expression throws.
/// * Finally it matches on required whitespace followed by the closing <c>THAT CONCLUDES THE MATTER.</c> keyword.
///
/// It should be noted that only a single expression is guarded rather than a block of statements.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// WITH THE GREATEST RESPECT, SUMMON RaffleVerdict WITH "Solicitor" IF YOU PLEASE.
///     WITH GRATITUDE
///         BEHOLD "A Blessing!"
///     MODIFIED RAPTURE, Grievance
///         BEHOLD WOVEN OF "A Hideous Curse: " AND Grievance IF YOU PLEASE.
/// THAT CONCLUDES THE MATTER.
/// </code>
/// the statement would be matched by the <c>TryCatch</c> parser, returning a <see cref="TryCatchNode"/> with:
/// * The operation set to the function call expression <c>SUMMON RaffleVerdict WITH "Solicitor" IF YOU PLEASE.</c>
/// * The success block containing one print statement.
/// * The exception block containing one print statement using the <c>Grievance</c> binding.
/// * The caught value name set to <c>Grievance</c>.
/// </para>
/// <para>
/// ### Assert Statements
/// The <c>Assert</c> parser matches on an assert statement, returning an <see cref="AssertNode"/>:
/// * It first matches on the <c>THE LAW IS</c> keyword.
/// * It then matches on required whitespace followed by a condition expression using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// * It then matches on required whitespace followed by the <c>THAT</c> keyword.
/// * Finally it matches on required whitespace followed by an error message expression using the <see cref="ExpressionParser"/><c>.Expression</c> parser.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// THE LAW IS PRE-ADAMITE score AND 0 THAT "Score must be positive"
/// THE LAW IS VERITY THAT errorMessage
/// </code>
/// both would be matched by the <c>Assert</c> parser, returning an <see cref="AssertNode"/> where:
/// * The first would have the condition set to the expression <c>PRE-ADAMITE score AND 0</c> and the error message set to the string literal <c>Score must be positive</c>.
/// * The second would have the condition set to the boolean literal <c>VERITY</c> and the error message set to the identifier <c>errorMessage</c>.
/// </para>
/// <para>
/// ### Functions
/// 5 parsers are included for function-related statements.
/// #### Parameter List
/// The <c>ParameterList</c> parser matches on the parameter clause of a function definition, returning a list of <see cref="Ast.TypedParameter"/> records.
/// It matches one of two forms:
/// * Functions with parameters: <c>UNDER THE TERMS OF</c> followed by one or more typed parameters in the form <c>&lt;name&gt; AS A &lt;type&gt;</c>, separated by <c>AND</c>.
/// * Functions with no parameters: <c>UNDER NO OBLIGATION</c>.
/// </para>
/// <para>
/// #### Return
/// The <c>Return</c> parser matches on a return statement with a value, returning a <see cref="ReturnNode"/>:
/// * It first matches on the <c>AND SO I FIND</c> keyword.
/// * It then matches on required whitespace followed by an expression for the return value.
/// </para>
/// <para>
/// #### Early Discharge (Early Return)
/// The <c>EarlyDischarge</c> parser matches on an early return with no value by matching the
/// <c>MY DUTY IS PREMATURELY DISCHARGED.</c> keyword, returning a <see cref="ReturnNode"/> with a <c>null</c> value.
/// </para>
/// <para>
/// #### Curse (Throw)
/// The <c>Curse</c> parser matches on a throw statement, returning a <see cref="ThrowNode"/>:
/// * It first matches on the <c>A HIDEOUS CURSE ON</c> keyword.
/// * It then matches on required whitespace followed by an expression for the value to throw.
/// </para>
/// <para>
/// #### Function Definition
/// The <c>FunctionDefinition</c> parser matches on a function definition, returning a <see cref="FunctionDefinitionNode"/>:
/// * It first matches on the <c>IT IS MY DUTY TO PERFORM</c> keyword.
/// * It then matches on required whitespace followed by an identifier for the function name.
/// * It then matches on the parameter clause using the <c>ParameterList</c> parser.
/// * It then optionally matches on the <c>TO FIND &lt;type&gt;</c> return-type clause, back-tracking if not matched.
///     * If no <c>TO FIND</c> clause is matched, the function is void.
/// * It then matches on zero or more body statements.
/// * Finally it matches on required whitespace followed by the closing <c>MY DUTY IS DISCHARGED.</c> keyword.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AS A PEER AND Liberals AS A PEER TO FIND PEER
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// the statement would be matched by the <c>FunctionDefinition</c> parser, returning a <see cref="FunctionDefinitionNode"/> with:
/// * The function name set to <c>TotalLords</c>.
/// * The parameters set to a list of two <see cref="Ast.TypedParameter"/> records: <c>Conservatives AS A PEER</c> and <c>Liberals AS A PEER</c>.
/// * The return type set to <see cref="LiteralType"/><c>.Integer</c>.
/// * The body containing one return statement.
/// </para>
/// <para>
/// ### Programme Structure
/// 2 parsers are included for top-level programme constructs.
/// #### Principal Block
/// The <c>PrincipalBlock</c> parser matches on a variable declaration block, returning a <see cref="PrincipalBlockNode"/>:
/// * It first matches on the <c>PRINCIPALS</c> keyword.
/// * It then matches on zero or more variable declarations using the <c>Declaration</c> parser.
/// * It then matches on required whitespace followed by the closing <c>THE CURTAIN RISES.</c> keyword.
///
/// A principal block groups variable declarations at the start of a programme before any executable statements.
/// </para>
/// <para>
/// #### Import
/// The <c>Import</c> parser matches on an import directive, returning an <see cref="ImportNode"/>:
/// * It first matches on the <c>PRAY ADMIT</c> keyword.
/// * It then matches on required whitespace followed by a string literal for the file path.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// PRAY ADMIT "mikado-punishments.topsy"
/// 
/// PRINCIPALS
///     PRAY WELCOME Defendant AS A YARN BEING "Edwin"
///     PRAY WELCOME JurySize AS A PEER BEING 12
/// THE CURTAIN RISES.
/// </code>
/// * The first statement would be matched by the <c>Import</c> parser, returning an <see cref="ImportNode"/> with the file path <c>mikado-punishments.topsy</c>.
/// * The second block would be matched by the <c>PrincipalBlock</c> parser, returning a <see cref="PrincipalBlockNode"/> with two declarations for the variables <c>Defendant</c> and <c>JurySize</c>.
/// </para>
/// <para>
/// ### Expression Statements
/// The <c>ExpressionStatementParser</c> parser matches on a standalone expression used as a statement, wrapping it in an
/// <see cref="ExpressionStatement"/> node.  It tries each expression type in order:
/// * <see cref="ExpressionParser.SummonExpression"/>: a function call.
/// * <see cref="ExpressionParser.ArrayIndexExpression"/>: an array element access (<c>VICTIM n ON arr</c>).
/// * <see cref="ExpressionParser.ArrayLengthExpression"/>: an array length expression (<c>RECKONING OF arr</c>).
/// * <see cref="ExpressionParser.PrefixExpression"/>: a prefix operator expression.
/// * <see cref="ExpressionParser.LiteralExpression"/>: a literal value.
/// * <see cref="ExpressionParser.ExpressionCast"/>: a non-mutating type cast (<c>AS IT WERE ... AS A</c>).
///
/// It should be noted that <see cref="ExpressionParser.IdentifierExpression"/> is intentionally excluded: a bare identifier would be ambiguous
/// with the start of an <c>Assignment</c> statement which also begins with an identifier.
/// </para>
/// <para>
/// ### Array Declarations
/// The <c>ArrayDeclaration</c> parser matches on array variable declarations, returning an <see cref="ArrayDeclarationNode"/>
/// with the variable name, element type, optional size, optional mutability modifier and optional initial values:
/// * It first matches the <c>PRAY WELCOME</c> keyword and required whitespace.
/// * It then matches the variable name identifier.
/// * It then matches <c>AS A</c>.
/// * It optionally matches a mutability modifier (<c>CONSERVATIVE</c> or <c>LIBERAL</c>), back-tracking if absent.
/// * It then matches the <c>LITTLE LIST OF</c> keyword.
///     * This does not match <c>A LITTLE LIST OF</c>, because the <c>A</c> was already consumed with <c>AS A</c> above.
/// * It optionally matches an integer size literal, back-tracking if absent.
/// * It then matches a scalar type keyword using <see cref="ExpressionParser.TypeKeyword"/>.
/// * Finally it tries to match the optional <c>BEING ... IF YOU PLEASE.</c> initialiser clause via the <c>ArrayInitialiser</c> parser.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "Pooh-Bah" AND "Ko-Ko" IF YOU PLEASE.
/// PRAY WELCOME slots AS A LITTLE LIST OF 3 YARN
/// </code>
/// both would return an <see cref="ArrayDeclarationNode"/> where:
/// * The first would have element type <see cref="LiteralType"/><c>.String</c>, no size, and two initial values.
/// * The second would have element type <see cref="LiteralType"/><c>.String</c>, size 3, and no initial values.
/// </para>
/// <para>
/// This parser must be tried before <c>Declaration</c> because both begin with <c>PRAY WELCOME</c>; the
/// <c>LITTLE LIST OF</c> keyword after <c>AS A</c> is the disambiguator.
/// </para>
/// <para>
/// ### Array Element Assignments
/// The <c>ArrayElementAssignment</c> parser matches on array element assignment statements, returning an
/// <see cref="ArrayElementAssignmentNode"/>:
/// * It first matches the <c>VICTIM</c> keyword.
/// * It then matches required whitespace followed by an index expression.
/// * It then matches required whitespace followed by the <c>ON</c> keyword.
/// * It then matches required whitespace followed by the array variable name identifier.
/// * It then matches required whitespace followed by the <c>IS APPOINTED</c> keyword.
/// * Finally it matches required whitespace followed by the new value expression.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// VICTIM 2 ON miscreants IS APPOINTED "Nanki-Poo"
/// </code>
/// the statement would be matched by the <c>ArrayElementAssignment</c> parser, returning an
/// <see cref="ArrayElementAssignmentNode"/> with index <c>2</c>, array name <c>miscreants</c> and value <c>Nanki-Poo</c>.
/// </para>
/// <para>
/// This parser must be tried before <c>Assignment</c> because both eventually match <c>IS APPOINTED</c>;
/// the leading <c>VICTIM</c> keyword disambiguates them.
/// </para>
/// <para>
/// ### Source Spans
/// Every statement node produced by this class carries a <see cref="BWHazel.TopsyTurvy.Ast.Node.Span"/> mapping the node back to its
/// position in the original (pre-processed) source.  The mechanism is identical to that used in <see cref="ExpressionParser"/>:
/// <see cref="ParserHelpers.CurrentOffset"/> is inserted as the first and last clause of every LINQ <c>from</c> chain, and
/// <see cref="ParserHelpers.BuildSpan"/> is called at node construction time with those two captured offsets.  The
/// <see cref="ParserHelpers.ActiveSourceMap"/> is set by <see cref="TopsyTurvyParser"/> before each parse and maps
/// pre-processed offsets back to original source line and column pairs.
///
/// The <c>FunctionDefinition</c> parser captures one pair of offsets per parameter, immediately before and
/// after each <c>Lexer.Identifier</c> consume in the parameter list, to populate the
/// <see cref="BWHazel.TopsyTurvy.Ast.TypedParameter.Span"/> of each <see cref="BWHazel.TopsyTurvy.Ast.TypedParameter"/>.
/// </para>
/// <para>
/// ### Committed Parse Semantics
/// Every parser in this class that matches the body of a block construct uses the committed-parse
/// pattern: once the opening keyword of a nested statement has been consumed, any subsequent failure is
/// propagated at the actual position of the error rather than backtracking to before the whitespace that
/// preceded the keyword.  As a result, syntax errors inside block bodies are reported at the line where
/// the error occurs, not at the opening line of the enclosing block.  Parsing still stops at the first
/// unrecoverable error: there is no multi-error recovery.
/// </para>
/// <para>
/// ### Main Entry Point
/// The <c>Statement</c> parser is the main entry point that tries every statement parser in turn:
/// * <c>PrincipalBlock</c>
/// * <c>ArrayDeclaration</c>
/// * <c>Declaration</c>
/// * <c>ArrayElementAssignment</c>
/// * <c>Assignment</c>
/// * <c>Print</c>
/// * <c>Input</c>
/// * <c>Conditional</c>
/// * <c>Switch</c>
/// * <c>Loop</c>
/// * <c>Guard</c>
/// * <c>TryCatch</c>
/// * <c>Assert</c>
/// * <c>FunctionDefinition</c>
/// * <c>Import</c>
/// * <c>EarlyDischarge</c>
/// * <c>Return</c>
/// * <c>Curse</c>
/// * <c>Break</c>
/// * <c>Continue</c>
/// * <c>ExpressionStatementParser</c>
/// </para>
/// </remarks>
public static class StatementParser
{
    /// <summary>
    /// Parses the element-initialiser list used by array declarations.
    /// </summary>
    /// <remarks>
    /// Matches <c>BEING &lt;expr&gt; AND &lt;expr&gt; ... IF YOU PLEASE.</c> and returns the collected expressions as a list.
    /// Returns an empty list when no <c>BEING</c> clause is present.
    /// </remarks>
    private static readonly TextParser<List<Expression>> ArrayInitialiser =
        (from beingKeyword in Ws(Lexer.Keyword("BEING"))
         from firstValue in Ws(ExpressionParser.Expression)
         from remainingValues in (
             from andKeyword in Ws(Lexer.Keyword("AND"))
             from value in Ws(ExpressionParser.Expression)
             select value
         )
         .Try()
         .Many()
         from closer in Ws(Lexer.Keyword("IF YOU PLEASE."))
         select new List<Expression>(remainingValues.Length + 1) { firstValue }
             .Concat(remainingValues)
             .ToList())
         .Try()
         .OptionalOrDefault(null!);

    /// <summary>
    /// Parses an array variable declaration.
    /// </summary>
    /// <remarks>
    /// Matches <c>PRAY WELCOME &lt;name&gt; AS A [CONSERVATIVE|LIBERAL] LITTLE LIST OF [size] &lt;type&gt; [BEING ... IF YOU PLEASE.]</c>
    /// and returns an <see cref="ArrayDeclarationNode"/>.  This parser must be tried before <see cref="Declaration"/>
    /// because both begin with <c>PRAY WELCOME</c>.
    /// </remarks>
    public static readonly TextParser<Statement> ArrayDeclaration =
        (from startOffset in CurrentOffset
         from _ in Lexer.Keyword("PRAY WELCOME")
         from variableName in Ws(Lexer.Identifier)
         from asAKeyword in Ws(Lexer.Keyword("AS A"))
         from mutabilityModifier in Ws(Lexer.Keyword("CONSERVATIVE")
             .Try()
             .Or(Lexer.Keyword("LIBERAL")
             .Try()))
             .OptionalOrDefault(null!)
         from littleListOfKeyword in Ws(Lexer.Keyword("LITTLE LIST OF"))
         from sizeValue in Ws(Lexer.IntegerLiteral).Select(value => (int?)value)
            .Try()
            .OptionalOrDefault(null)
         from elementType in Ws(ExpressionParser.TypeKeyword)
         from initialValues in ArrayInitialiser
         from endOffset in CurrentOffset
         select (Statement)new ArrayDeclarationNode()
         {
             Name = variableName,
             ElementType = elementType,
             Size = sizeValue,
             IsConstant = mutabilityModifier == "CONSERVATIVE",
             InitialValues = initialValues ?? [],
             Span = BuildSpan(startOffset, endOffset)
         })
         .Try();

    /// <summary>
    /// Parses an array element assignment statement.
    /// </summary>
    /// <remarks>
    /// Matches <c>VICTIM &lt;index&gt; ON &lt;array&gt; IS APPOINTED &lt;value&gt;</c> and returns an
    /// <see cref="ArrayElementAssignmentNode"/>.  This parser must be tried before <see cref="Assignment"/> because both
    /// eventually match <c>IS APPOINTED</c>; the <c>VICTIM</c> prefix disambiguates them.
    /// </remarks>
    public static readonly TextParser<Statement> ArrayElementAssignment =
        (from startOffset in CurrentOffset
         from victimKeyword in Lexer.Keyword("VICTIM")
         from index in Ws(ExpressionParser.Expression)
         from onKeyword in Ws(Lexer.Keyword("ON"))
         from arrayName in Ws(ExpressionParser.ArrayNameParser)
         from isAppointedKeyword in Ws(Lexer.Keyword("IS APPOINTED"))
         from value in Ws(ExpressionParser.Expression)
         from endOffset in CurrentOffset
         select (Statement)new ArrayElementAssignmentNode()
         {
             Index = index,
             ArrayName = arrayName,
             Value = value,
             Span = BuildSpan(startOffset, endOffset)
         })
         .Try();

    /// <summary>
    /// Parses a variable declaration.
    /// </summary>
    public static readonly TextParser<Statement> Declaration =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("PRAY WELCOME")
        from variableName in Ws(Lexer.Identifier)
        from asAKeyword in Ws(Lexer.Keyword("AS A"))
        from mutabilityModifier in Ws(Lexer.Keyword("CONSERVATIVE")
            .Try()
            .Or(Lexer.Keyword("LIBERAL")
            .Try()))
            .OptionalOrDefault(null!)
        from variableType in Ws(ExpressionParser.TypeKeyword)
        from initialValue in Ws(Lexer.Keyword("BEING")
            .IgnoreThen(Ws(ExpressionParser.Expression)))
            .Try()
            .OptionalOrDefault(null!)
        from endOffset in CurrentOffset
        select (Statement)new DeclarationNode()
        {
            Name = variableName,
            Type = variableType,
            IsConstant = mutabilityModifier == "CONSERVATIVE",
            InitialValue = initialValue,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an assignment statement.
    /// </summary>
    public static readonly TextParser<Statement> Assignment =
        (from startOffset in CurrentOffset
         from variableName in Lexer.Identifier
         from _ in Ws(Lexer.Keyword("IS APPOINTED"))
         from newValue in Ws(ExpressionParser.Expression)
         from endOffset in CurrentOffset
         select (Statement)new AssignmentNode()
         {
             Target = variableName,
             Value = newValue,
             Span = BuildSpan(startOffset, endOffset)
         })
         .Try();

    /// <summary>
    /// Parses an output statement.
    /// </summary>
    public static readonly TextParser<Statement> Print =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("BEHOLD")
        from expression in Ws(ExpressionParser.Expression)
        from withoutCeremonyKeyword in Ws(Lexer.Keyword("WITHOUT CEREMONY"))
            .Try()
            .OptionalOrDefault(null!)
        from endOffset in CurrentOffset
        select (Statement)new PrintNode()
        {
            Expression = expression,
            SuppressNewline = withoutCeremonyKeyword is not null,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an input statement.
    /// </summary>
    public static readonly TextParser<Statement> Input =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("PRAY TELL")
        from variableName in Ws(Lexer.Identifier)
        from endOffset in CurrentOffset
        select (Statement)new InputNode()
        {
            Target = variableName,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses the body of a conditional.
    /// </summary>
    public static readonly TextParser<ConditionalBodyInfo> ConditionalBody =
        from trueBlockKeyword in Ws(Lexer.Keyword("QUITE SO.")
            .Named("QUITE SO. (then-block)"))
        from trueBlock in WsMany(Parse.Ref(() => Statement!))
        from elseIfBlocks in (
            from _ in Ws(Lexer.Keyword("OR, IF NOT,"))
            from condition in Ws(ExpressionParser.Expression)
            from block in WsMany(Parse.Ref(() => Statement!))
            select new ElseIfBranch(condition, [.. block])
        )
        .Try()
        .Many()
        from elseBlock in (
            from _ in Ws(Lexer.Keyword("OTHERWISE,"))
            from block in WsMany(Parse.Ref(() => Statement!))
            select (IReadOnlyList<Statement>)[.. block]
        )
        .Try()
        .OptionalOrDefault(null!)
        select new ConditionalBodyInfo(
            (IReadOnlyList<Statement>)[.. trueBlock],
            (IReadOnlyList<ElseIfBranch>)[.. elseIfBlocks],
            elseBlock ?? []);

    /// <summary>
    /// Parses a conditional statement.
    /// </summary>
    public static readonly TextParser<Statement> Conditional =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("SHOULD IT TRANSPIRE THAT")
        from condition in Ws(ExpressionParser.Expression)
        from body in ConditionalBody
        from closer in Ws(Lexer.Keyword("SO MUCH FOR THAT.")
            .Named("SO MUCH FOR THAT. (end of conditional)"))
        from endOffset in CurrentOffset
        select (Statement)new ConditionalNode
        {
            Condition = condition,
            TrueBlock = body.TrueBlock,
            ElseIfs = body.ElseIfs,
            ElseBlock = body.ElseBlock,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses the body of a switch.
    /// </summary>
    public static readonly TextParser<SwitchBodyInfo> SwitchBody =
        from cases in (
            from _ in Ws(Lexer.Keyword("WHEN ACTING AS"))
            from literalValue in Ws(
                Lexer.NullLiteral.Select(value => (object?)value)
                    .Or(Lexer.BooleanLiteral.Select(value => (object?)value))
                    .Or(Lexer.FloatLiteral.Select(value => (object?)value))
                    .Or(Lexer.IntegerLiteral.Select(value => (object?)value))
                    .Or(Lexer.StringLiteral.Select(value => (object?)value)))
            from caseBody in WsMany(Parse.Ref(() => Statement!))
            select new SwitchCase(literalValue, [.. caseBody])
        )
        .Try()
        .Many()
        from defaultCase in (
            from _ in Ws(Lexer.Keyword("FAILING ALL OF THE ABOVE,"))
            from block in WsMany(Parse.Ref(() => Statement!))
            select (IReadOnlyList<Statement>)[.. block]
        )
        .Try()
        .OptionalOrDefault(null!)
        select new SwitchBodyInfo(
            (IReadOnlyList<SwitchCase>)[.. cases],
            defaultCase ?? (IReadOnlyList<Statement>)[]);

    /// <summary>
    /// Parses a switch statement.
    /// </summary>
    public static readonly TextParser<Statement> Switch =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("IN WHICH CAPACITY?")
        from expression in Ws(ExpressionParser.Expression)
        from body in SwitchBody
        from closer in Ws(Lexer.Keyword("NOTHING COULD BE MORE SATISFACTORY.")
            .Named("NOTHING COULD BE MORE SATISFACTORY. (end of switch)"))
        from endOffset in CurrentOffset
        select (Statement)new SwitchNode()
        {
            Expression = expression,
            Cases = body.Cases,
            DefaultBlock = body.DefaultBlock,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses the optional <c>BY &lt;expr&gt;</c> step clause for ascending/descending loops.
    /// </summary>
    private static readonly TextParser<Expression?> LoopStepParser =
        Ws(Lexer.Keyword("BY")).IgnoreThen(Ws(ExpressionParser.Expression))
            .Select(step => (Expression?)step)
            .Try()
            .OptionalOrDefault(null);

    /// <summary>
    /// Parses the loop-type clause following the label.
    /// </summary>
    private static readonly TextParser<LoopDefinition> LoopTypeParser =
        (from _ in Ws(Lexer.Keyword("ASCENDING"))
         from loopVariable in Ws(Lexer.Identifier)
         from loopStep in LoopStepParser
         from untilKeyword in Ws(Lexer.Keyword("UNTIL"))
         from loopCondition in Ws(ExpressionParser.Expression)
         select new LoopDefinition(LoopType.Ascending, loopCondition, loopVariable, loopStep))
            .Try()
            .Or((from _ in Ws(Lexer.Keyword("DESCENDING"))
                from loopVariable in Ws(Lexer.Identifier)
                from loopStep in LoopStepParser
                from untilKeyword in Ws(Lexer.Keyword("UNTIL"))
                from loopCondition in Ws(ExpressionParser.Expression)
                select new LoopDefinition(LoopType.Descending, loopCondition, loopVariable, loopStep))
                    .Try())
            .Or((from _ in Ws(Lexer.Keyword("WHILST"))
                from loopCondition in Ws(ExpressionParser.Expression)
                select new LoopDefinition(LoopType.Whilst, loopCondition, null))
                    .Try())
            .Or(Parse.Return(new LoopDefinition(LoopType.Infinite, null, null)));

    /// <summary>
    /// Parses a break statement.
    /// </summary>
    public static readonly TextParser<Statement> Break =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("THAT WILL DO.")
        from endOffset in CurrentOffset
        select (Statement)new BreakNode() { Span = BuildSpan(startOffset, endOffset) };

    /// <summary>
    /// Parses a continue statement.
    /// </summary>
    public static readonly TextParser<Statement> Continue =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("ONCE MORE.")
        from endOffset in CurrentOffset
        select (Statement)new ContinueNode() { Span = BuildSpan(startOffset, endOffset) };

    /// <summary>
    /// Parses a loop.
    /// </summary>
    public static readonly TextParser<Statement> Loop =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("BY A LEGAL FICTION")
        from label in Ws(Lexer.Keyword("KNOWN AS").IgnoreThen(Ws(Lexer.Identifier)))
                         .Try().OptionalOrDefault(null!)
        from loopDefinition in LoopTypeParser
        from body in WsMany(Parse.Ref(() => Statement!))
        from closer in Ws(Lexer.Keyword("THE TERM EXPIRES.")
            .Named("THE TERM EXPIRES. (end of loop)"))
        from endOffset in CurrentOffset
        select (Statement)new LoopNode()
        {
            Label = label,
            Type = loopDefinition.Type,
            Condition = loopDefinition.Condition,
            LoopVariable = loopDefinition.Variable,
            Step = loopDefinition.Step,
            Body = [.. body],
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an exception-handling block.
    /// </summary>
    public static readonly TextParser<Statement> TryCatch =
        from startOffset in CurrentOffset
        from opener in Lexer.Keyword("WITH THE GREATEST RESPECT,")
        from throwableExpression in Ws(ExpressionParser.Expression)
        from withGratitudeKeyword in Ws(Lexer.Keyword("WITH GRATITUDE"))
        from successBlock in WsMany(Parse.Ref(() => Statement!))
        from modifiedRaptureKeyword in Ws(Lexer.Keyword("MODIFIED RAPTURE"))
        from _ in Character.EqualTo(',')
        from caughtName in Ws(Lexer.Identifier)
        from catchBlock in WsMany(Parse.Ref(() => Statement!))
        from closer in Ws(Lexer.Keyword("THAT CONCLUDES THE MATTER.").Named("THAT CONCLUDES THE MATTER. (end of try/catch)"))
        from endOffset in CurrentOffset
        select (Statement)new TryCatchNode()
        {
            Operation = throwableExpression,
            SuccessBlock = [.. successBlock],
            ExceptionBlock = [.. catchBlock],
            CaughtValueName = caughtName,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses a guard clause.
    /// </summary>
    public static readonly TextParser<Statement> Guard =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("YEOMAN")
        from condition in Ws(ExpressionParser.Expression)
        from otherwiseKeyword in Ws(Lexer.Keyword("OTHERWISE,").Named("OTHERWISE, (guard body)"))
        from body in WsMany(Parse.Ref(() => Statement!))
        from closer in Ws(Lexer.Keyword("UNDER ORDERS.").Named("UNDER ORDERS. (end of guard)"))
        from endOffset in CurrentOffset
        select (Statement)new GuardNode()
        {
            Condition = condition,
            ElseBlock = [.. body],
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses the parameter list of a function definition.
    /// </summary>
    private static readonly TextParser<List<TypedParameter>> ParameterList =
        (from underTermsKeyword in Ws(Lexer.Keyword("UNDER THE TERMS OF"))
         from firstParameterStart in Ws(CurrentOffset)
         from firstParameter in Lexer.Identifier
         from firstParameterEnd in CurrentOffset
         from firstAsAKeyword in Ws(Lexer.Keyword("AS A"))
         from firstParameterType in Ws(ExpressionParser.TypeKeyword)
         from remainingParameters in (
             from andKeyword in Ws(Lexer.Keyword("AND"))
             from parameterStart in Ws(CurrentOffset)
             from parameter in Lexer.Identifier
             from parameterEnd in CurrentOffset
             from parameterAsAKeyword in Ws(Lexer.Keyword("AS A"))
             from parameterType in Ws(ExpressionParser.TypeKeyword)
             select new TypedParameter(parameter, parameterType, BuildSpan(parameterStart, parameterEnd))
         )
         .Try()
         .Many()
         select new List<TypedParameter>(remainingParameters.Length + 1)
         {
            new TypedParameter(firstParameter, firstParameterType, BuildSpan(firstParameterStart, firstParameterEnd))
        }
        .Concat(remainingParameters)
        .ToList())
        .Or(Ws(Lexer.Keyword("UNDER NO OBLIGATION"))
            .Select(_ => new List<TypedParameter>()));

    /// <summary>
    /// Parses a return statement.
    /// </summary>
    public static readonly TextParser<Statement> Return =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("AND SO I FIND")
        from returnValue in Ws(ExpressionParser.Expression)
        from endOffset in CurrentOffset
        select (Statement)new ReturnNode()
        {
            Value = returnValue,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an early no-value return.
    /// </summary>
    public static readonly TextParser<Statement> EarlyDischarge =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("MY DUTY IS PREMATURELY DISCHARGED.")
        from endOffset in CurrentOffset
        select (Statement)new ReturnNode() { Value = null, Span = BuildSpan(startOffset, endOffset) };

    /// <summary>
    /// Parses a throw statement.
    /// </summary>
    public static readonly TextParser<Statement> Curse =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("A HIDEOUS CURSE ON")
        from curseValue in Ws(ExpressionParser.Expression)
        from endOffset in CurrentOffset
        select (Statement)new ThrowNode()
        {
            Value = curseValue,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an assert statement.
    /// </summary>
    public static readonly TextParser<Statement> Assert =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("THE LAW IS")
        from condition in Ws(ExpressionParser.Expression)
        from thatKeyword in Ws(Lexer.Keyword("THAT").Named("THAT (assert error message)"))
        from errorMessage in Ws(ExpressionParser.Expression)
        from endOffset in CurrentOffset
        select (Statement)new AssertNode()
        {
            Condition = condition,
            ErrorMessage = errorMessage,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses a function definition.
    /// </summary>
    public static readonly TextParser<Statement> FunctionDefinition =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("IT IS MY DUTY TO PERFORM")
        from functionName in Ws(Lexer.Identifier)
        from parameters in ParameterList
        from returnType in Ws(Lexer.Keyword("TO FIND").IgnoreThen(Ws(ExpressionParser.TypeKeyword)))
            .Select(type => (LiteralType?)type)
            .Try()
            .OptionalOrDefault(null)
        from body in WsMany(Parse.Ref(() => Statement!))
        from closer in Ws(Lexer.Keyword("MY DUTY IS DISCHARGED.")
            .Named("MY DUTY IS DISCHARGED. (end of function)"))
        from endOffset in CurrentOffset
        select (Statement)new FunctionDefinitionNode()
        {
            Name = functionName,
            Parameters = [.. parameters],
            ReturnType = returnType,
            Body = [.. body],
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses a variable-declaration block.
    /// </summary>
    /// <remarks>
    /// Accepts both scalar declarations (<see cref="Declaration"/>) and array declarations
    /// (<see cref="ArrayDeclaration"/>) in any interleaved order, preserving source order in
    /// <see cref="PrincipalBlockNode.Declarations"/>.
    /// </remarks>
    public static readonly TextParser<Statement> PrincipalBlock =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("PRINCIPALS")
        from declarations in WsMany(ArrayDeclaration.Or(Declaration))
        from closer in Ws(Lexer.Keyword("THE CURTAIN RISES.")
            .Named("THE CURTAIN RISES. (end of declarations)"))
        from endOffset in CurrentOffset
        select (Statement)new PrincipalBlockNode()
        {
            Declarations = declarations,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses an import directive.
    /// </summary>
    public static readonly TextParser<Statement> Import =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("PRAY ADMIT")
        from importPath in Ws(Lexer.StringLiteral)
        from endOffset in CurrentOffset
        select (Statement)new ImportNode()
        {
            FilePath = importPath,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses a standalone expression as a statement.
    /// </summary>
    public static readonly TextParser<Statement> ExpressionStatementParser =
        ExpressionParser.SummonExpression
            .Or(ExpressionParser.ArrayIndexExpression)
            .Or(ExpressionParser.ArrayLengthExpression)
            .Or(ExpressionParser.PrefixExpression)
            .Or(ExpressionParser.LiteralExpression)
            .Or(ExpressionParser.ExpressionCast)
            .Select(expression => (Statement)new ExpressionStatement()
                {
                    Expression = expression,
                    Span = expression.Span
                });

    /// <summary>
    /// Parses any single Topsy Turvy statement.
    /// </summary>
    public static readonly TextParser<Statement> Statement =
        PrincipalBlock
            .Or(ArrayDeclaration)
            .Or(Declaration)
            .Or(ArrayElementAssignment)
            .Or(Assignment)
            .Or(Print)
            .Or(Input)
            .Or(Conditional)
            .Or(Switch)
            .Or(Loop)
            .Or(Guard)
            .Or(TryCatch)
            .Or(FunctionDefinition)
            .Or(Import)
            .Or(EarlyDischarge)
            .Or(Return)
            .Or(Curse)
            .Or(Assert)
            .Or(Break)
            .Or(Continue)
            .Or(ExpressionStatementParser);
}
