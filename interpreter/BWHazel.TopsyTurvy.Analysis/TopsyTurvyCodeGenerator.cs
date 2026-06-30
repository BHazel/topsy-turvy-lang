using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Generates valid Topsy Turvy source code from a <see cref="ProgramNode"/> AST.
/// </summary>
/// <remarks>
/// The generated source is formatted with 2-space indentation and passes
/// <c>TopsyTurvyParser.TryParse</c> without errors.
/// </remarks>
public sealed class TopsyTurvyCodeGenerator
{
    /// <summary>
    /// Generates Topsy Turvy source code for the given programme.
    /// </summary>
    /// <param name="program">The root AST node to generate source from.</param>
    /// <returns>A string containing the complete, formatted Topsy Turvy source.</returns>
    public string Generate(ProgramNode program)
    {
        StringBuilder generatedCodeBuilder = new();

        generatedCodeBuilder.Append("HARK! \"");
        generatedCodeBuilder.Append(program.Title);
        generatedCodeBuilder.AppendLine("\"");

        if (program.Subtitle is not null)
        {
            generatedCodeBuilder.Append("  or, \"");
            generatedCodeBuilder.Append(program.Subtitle);
            generatedCodeBuilder.AppendLine("\"");
        }

        generatedCodeBuilder.AppendLine();
        foreach (Statement statement in program.Statements)
        {
            this.WriteStatement(statement, depth: 0, generatedCodeBuilder);
        }

        generatedCodeBuilder.AppendLine("FINALE.");
        return generatedCodeBuilder.ToString();
    }

    /// <summary>
    /// Writes a single statement at the given indentation depth.
    /// </summary>
    /// <param name="statement">The statement to emit.</param>
    /// <param name="depth">The current indentation level, 2 spaces per level.</param>
    /// <param name="generatedCodeBuilder">The <see cref="StringBuilder"/> to append to.</param>
    private void WriteStatement(Statement statement, int depth, StringBuilder generatedCodeBuilder)
    {
        string indent = new(' ', depth * 2);
        switch (statement)
        {
            case PrincipalBlockNode principals:
                generatedCodeBuilder.AppendLine($"{indent}PRINCIPALS");
                foreach (Statement declaration in principals.Declarations)
                {
                    this.WriteStatement(declaration, depth + 1, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}THE CURTAIN RISES.");
                generatedCodeBuilder.AppendLine();
                break;
            case DeclarationNode declaration:
                string constantModifier = declaration.IsConstant ? "CONSERVATIVE " : string.Empty;
                string typeName = this.TypeKeyword(declaration.Type);
                generatedCodeBuilder.Append($"{indent}PRAY WELCOME {declaration.Name} AS A {constantModifier}{typeName}");
                if (declaration.InitialValue is not null)
                {
                    generatedCodeBuilder.Append(" BEING ");
                    this.WriteExpression(declaration.InitialValue, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine();
                break;
            case ArrayDeclarationNode arrayDeclaration:
                string arrayConstantModifier = arrayDeclaration.IsConstant ? "CONSERVATIVE " : string.Empty;
                string elementTypeName = this.TypeKeyword(arrayDeclaration.ElementType);
                generatedCodeBuilder.Append($"{indent}PRAY WELCOME {arrayDeclaration.Name} AS A {arrayConstantModifier}LITTLE LIST OF");
                if (arrayDeclaration.Size.HasValue)
                {
                    generatedCodeBuilder.Append($" {arrayDeclaration.Size.Value}");
                }

                generatedCodeBuilder.Append($" {elementTypeName}");
                if (arrayDeclaration.InitialValues.Count > 0)
                {
                    generatedCodeBuilder.Append(" BEING ");
                    for (int i = 0; i < arrayDeclaration.InitialValues.Count; i++)
                    {
                        if (i > 0)
                        {
                            generatedCodeBuilder.Append(" AND ");
                        }

                        this.WriteExpression(arrayDeclaration.InitialValues[i], generatedCodeBuilder);
                    }

                    generatedCodeBuilder.Append(" IF YOU PLEASE.");
                }

                generatedCodeBuilder.AppendLine();
                break;
            case AssignmentNode assignment:
                generatedCodeBuilder.Append($"{indent}{assignment.Target} IS APPOINTED ");
                this.WriteExpression(assignment.Value, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                break;
            case ArrayElementAssignmentNode arrayElementAssignment:
                generatedCodeBuilder.Append($"{indent}VICTIM ");
                this.WriteExpression(arrayElementAssignment.Index, generatedCodeBuilder);
                generatedCodeBuilder.Append($" ON {arrayElementAssignment.ArrayName} IS APPOINTED ");
                this.WriteExpression(arrayElementAssignment.Value, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                break;
            case PrintNode print:
                generatedCodeBuilder.Append($"{indent}BEHOLD ");
                this.WriteExpression(print.Expression, generatedCodeBuilder);
                if (print.SuppressNewline)
                {
                    generatedCodeBuilder.Append(" WITHOUT CEREMONY");
                }

                generatedCodeBuilder.AppendLine();
                break;
            case InputNode input:
                generatedCodeBuilder.AppendLine($"{indent}PRAY TELL {input.Target}");
                break;
            case ConditionalNode conditional:
                generatedCodeBuilder.Append($"{indent}SHOULD IT TRANSPIRE THAT ");
                this.WriteExpression(conditional.Condition, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                generatedCodeBuilder.AppendLine($"{indent}QUITE SO.");
                foreach (Statement trueBlockStatement in conditional.TrueBlock)
                {
                    this.WriteStatement(trueBlockStatement, depth + 1, generatedCodeBuilder);
                }

                foreach (ElseIfBranch elseIf in conditional.ElseIfs)
                {
                    generatedCodeBuilder.Append($"{indent}OR, IF NOT, ");
                    this.WriteExpression(elseIf.Condition, generatedCodeBuilder);
                    generatedCodeBuilder.AppendLine();
                    foreach (Statement elseIfBlockStatement in elseIf.Block)
                    {
                        this.WriteStatement(elseIfBlockStatement, depth + 1, generatedCodeBuilder);
                    }
                }

                if (conditional.ElseBlock.Count > 0)
                {
                    generatedCodeBuilder.AppendLine($"{indent}OTHERWISE,");
                    foreach (Statement elseBlockStatement in conditional.ElseBlock)
                    {
                        this.WriteStatement(elseBlockStatement, depth + 1, generatedCodeBuilder);
                    }
                }

                generatedCodeBuilder.AppendLine($"{indent}SO MUCH FOR THAT.");
                break;
            case LoopNode loop:
                generatedCodeBuilder.Append($"{indent}BY A LEGAL FICTION");
                if (loop.Label is not null)
                {
                    generatedCodeBuilder.Append($" KNOWN AS {loop.Label}");
                }

                switch (loop.Type)
                {
                    case LoopType.Ascending:
                        generatedCodeBuilder.Append($" ASCENDING {loop.LoopVariable} UNTIL ");
                        this.WriteExpression(loop.Condition!, generatedCodeBuilder);
                        break;
                    case LoopType.Descending:
                        generatedCodeBuilder.Append($" DESCENDING {loop.LoopVariable} UNTIL ");
                        this.WriteExpression(loop.Condition!, generatedCodeBuilder);
                        break;
                    case LoopType.Whilst:
                        generatedCodeBuilder.Append(" WHILST ");
                        this.WriteExpression(loop.Condition!, generatedCodeBuilder);
                        break;
                }

                generatedCodeBuilder.AppendLine();
                foreach (Statement loopBodyStatement in loop.Body)
                {
                    this.WriteStatement(loopBodyStatement, depth + 1, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}THE TERM EXPIRES.");
                break;
            case SwitchNode switchBlock:
                generatedCodeBuilder.Append($"{indent}IN WHICH CAPACITY? ");
                this.WriteExpression(switchBlock.Expression, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                foreach (SwitchCase switchCase in switchBlock.Cases)
                {
                    string caseLiteral = this.FormatSwitchLiteral(switchCase.Literal);
                    generatedCodeBuilder.AppendLine($"{indent}  WHEN ACTING AS {caseLiteral}");
                    foreach (Statement caseStatement in switchCase.Block)
                    {
                        this.WriteStatement(caseStatement, depth + 2, generatedCodeBuilder);
                    }
                }

                if (switchBlock.DefaultBlock.Count > 0)
                {
                    generatedCodeBuilder.AppendLine($"{indent}  FAILING ALL OF THE ABOVE,");
                    foreach (Statement defaultCaseStatement in switchBlock.DefaultBlock)
                    {
                        this.WriteStatement(defaultCaseStatement, depth + 2, generatedCodeBuilder);
                    }
                }

                generatedCodeBuilder.AppendLine($"{indent}NOTHING COULD BE MORE SATISFACTORY.");
                break;
            case FunctionDefinitionNode functionDefinition:
                generatedCodeBuilder.Append($"{indent}IT IS MY DUTY TO PERFORM {functionDefinition.Name}");
                if (functionDefinition.Parameters.Count > 0)
                {
                    generatedCodeBuilder.Append(" UNDER THE TERMS OF ");
                    for (int i = 0; i < functionDefinition.Parameters.Count; i++)
                    {
                        if (i > 0)
                        {
                            generatedCodeBuilder.Append(" AND ");
                        }

                        TypedParameter parameter = functionDefinition.Parameters[i];
                        generatedCodeBuilder.Append($"{parameter.Name} AS A {this.TypeKeyword(parameter.Type)}");
                    }
                }
                else
                {
                    generatedCodeBuilder.Append(" UNDER NO OBLIGATION");
                }

                if (functionDefinition.ReturnType.HasValue)
                {
                    generatedCodeBuilder.Append($" TO FIND {this.TypeKeyword(functionDefinition.ReturnType.Value)}");
                }

                generatedCodeBuilder.AppendLine();
                foreach (Statement functionBodyStatement in functionDefinition.Body)
                {
                    this.WriteStatement(functionBodyStatement, depth + 1, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}MY DUTY IS DISCHARGED.");
                break;
            case ReturnNode returnNode:
                if (returnNode.Value is not null)
                {
                    generatedCodeBuilder.Append($"{indent}AND SO I FIND ");
                    this.WriteExpression(returnNode.Value, generatedCodeBuilder);
                    generatedCodeBuilder.AppendLine();
                }
                else
                {
                    generatedCodeBuilder.AppendLine($"{indent}MY DUTY IS PREMATURELY DISCHARGED.");
                }

                break;
            case TryCatchNode tryCatch:
                generatedCodeBuilder.Append($"{indent}WITH THE GREATEST RESPECT, ");
                this.WriteExpression(tryCatch.Operation, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                generatedCodeBuilder.AppendLine($"{indent}  WITH GRATITUDE");
                foreach (Statement successBlockStatement in tryCatch.SuccessBlock)
                {
                    this.WriteStatement(successBlockStatement, depth + 2, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}  MODIFIED RAPTURE, {tryCatch.CaughtValueName}");
                foreach (Statement exceptionBlockStatement in tryCatch.ExceptionBlock)
                {
                    this.WriteStatement(exceptionBlockStatement, depth + 2, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}THAT CONCLUDES THE MATTER.");
                break;
            case GuardNode guard:
                generatedCodeBuilder.Append($"{indent}YEOMAN ");
                this.WriteExpression(guard.Condition, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                generatedCodeBuilder.AppendLine($"{indent}OTHERWISE,");
                foreach (Statement elseBlockStatement in guard.ElseBlock)
                {
                    this.WriteStatement(elseBlockStatement, depth + 1, generatedCodeBuilder);
                }

                generatedCodeBuilder.AppendLine($"{indent}UNDER ORDERS.");
                break;
            case AssertNode assert:
                generatedCodeBuilder.Append($"{indent}THE LAW IS ");
                this.WriteExpression(assert.Condition, generatedCodeBuilder);
                generatedCodeBuilder.Append(" THAT ");
                this.WriteExpression(assert.ErrorMessage, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                break;
            case ThrowNode throwNode:
                generatedCodeBuilder.Append($"{indent}A HIDEOUS CURSE ON ");
                this.WriteExpression(throwNode.Value, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                break;
            case BreakNode:
                generatedCodeBuilder.AppendLine($"{indent}THAT WILL DO.");
                break;
            case ContinueNode:
                generatedCodeBuilder.AppendLine($"{indent}ONCE MORE.");
                break;
            case ImportNode import:
                generatedCodeBuilder.AppendLine($"{indent}PRAY ADMIT \"{import.FilePath}\"");
                break;
            case ExpressionStatement expressionStatement:
                this.WriteExpression(expressionStatement.Expression, generatedCodeBuilder);
                generatedCodeBuilder.AppendLine();
                break;
        }
    }

    /// <summary>
    /// Writes an expression inline with no newline appended.
    /// </summary>
    /// <param name="expression">The expression to emit.</param>
    /// <param name="generatedCodeBuilder">The <see cref="StringBuilder"/> to append to.</param>
    private void WriteExpression(Expression expression, StringBuilder generatedCodeBuilder)
    {
        switch (expression)
        {
            case LiteralNode literal:
                this.WriteLiteral(literal, generatedCodeBuilder);
                break;
            case IdentifierNode identifier:
                generatedCodeBuilder.Append(identifier.Name);
                break;
            case PrefixExpressionNode prefix:
                this.WritePrefixExpression(prefix, generatedCodeBuilder);
                break;
            case TernaryExpressionNode ternary:
                this.WriteExpression(ternary.TrueValue, generatedCodeBuilder);
                generatedCodeBuilder.Append(" SHOULD IT TRANSPIRE THAT ");
                this.WriteExpression(ternary.Condition, generatedCodeBuilder);
                generatedCodeBuilder.Append(" OTHERWISE, ");
                this.WriteExpression(ternary.FalseValue, generatedCodeBuilder);
                break;
            case ArrayIndexNode arrayIndex:
                generatedCodeBuilder.Append("VICTIM ");
                this.WriteExpression(arrayIndex.Index, generatedCodeBuilder);
                generatedCodeBuilder.Append($" ON {arrayIndex.ArrayName}");
                break;
            case ArrayLengthNode arrayLength:
                generatedCodeBuilder.Append($"RECKONING OF {arrayLength.ArrayName}");
                break;
            case ExpressionCastNode cast:
                generatedCodeBuilder.Append("AS IT WERE ");
                this.WriteExpression(cast.Expression, generatedCodeBuilder);
                generatedCodeBuilder.Append($" AS A {this.TypeKeyword(cast.NewType)}");
                break;
        }
    }

    /// <summary>
    /// Writes a literal value in its source representation.
    /// </summary>
    /// <param name="literal">The literal node to emit.</param>
    /// <param name="generatedCodeBuilder">The <see cref="StringBuilder"/> to append to.</param>
    private void WriteLiteral(LiteralNode literal, StringBuilder generatedCodeBuilder)
    {
        switch (literal.Type)
        {
            case LiteralType.String:
                generatedCodeBuilder.Append('"');
                generatedCodeBuilder.Append(literal.Value?.ToString() ?? string.Empty);
                generatedCodeBuilder.Append('"');
                break;
            case LiteralType.Boolean:
                generatedCodeBuilder.Append(literal.Value is true
                    ? Keywords.Literals.Verity
                    : Keywords.Literals.Nay);
                break;
            case LiteralType.Null:
                generatedCodeBuilder.Append(Keywords.Literals.Naught);
                break;
            case LiteralType.Char:
                generatedCodeBuilder.Append('\'');
                generatedCodeBuilder.Append(literal.Value?.ToString() ?? string.Empty);
                generatedCodeBuilder.Append('\'');
                break;
            case LiteralType.Double:
            case LiteralType.Single:
                string floatingPointString = literal.Value is IFormattable formattable
                    ? formattable.ToString("G", CultureInfo.InvariantCulture)
                    : literal.Value?.ToString() ?? "0";
                
                if (!floatingPointString.Contains('.') && !floatingPointString.Contains('E'))
                {
                    floatingPointString += ".0";
                }

                generatedCodeBuilder.Append(floatingPointString);
                break;
            default:
                generatedCodeBuilder.Append(Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "0");
                break;
        }
    }

    /// <summary>
    /// Writes a prefix expression, including variadic forms and function calls.
    /// </summary>
    /// <param name="prefix">The prefix expression node to emit.</param>
    /// <param name="generatedCodeBuilder">The <see cref="StringBuilder"/> to append to.</param>
    private void WritePrefixExpression(PrefixExpressionNode prefix, StringBuilder generatedCodeBuilder)
    {
        switch (prefix.Operator)
        {
            case Operator.WovenOf:
            case Operator.AllOf:
            case Operator.AnyOf:
                generatedCodeBuilder.Append(this.OperatorKeyword(prefix.Operator));
                generatedCodeBuilder.Append(' ');
                for (int i = 0; i < prefix.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        generatedCodeBuilder.Append(" AND ");
                    }

                    this.WriteExpression(prefix.Arguments[i], generatedCodeBuilder);
                }

                generatedCodeBuilder.Append(" IF YOU PLEASE.");
                break;
            case Operator.Summon:
                generatedCodeBuilder.Append("SUMMON ");
                this.WriteExpression(prefix.Arguments[0], generatedCodeBuilder);
                if (prefix.Arguments.Count > 1)
                {
                    generatedCodeBuilder.Append(" WITH ");
                    for (int i = 1; i < prefix.Arguments.Count; i++)
                    {
                        if (i > 1)
                        {
                            generatedCodeBuilder.Append(" AND ");
                        }

                        this.WriteExpression(prefix.Arguments[i], generatedCodeBuilder);
                    }
                }
                else
                {
                    generatedCodeBuilder.Append(" WITH NOTHING");
                }

                generatedCodeBuilder.Append(" IF YOU PLEASE.");
                break;
            case Operator.HardlyEver:
            case Operator.InversionOf:
            case Operator.TranspositionUp:
            case Operator.TranspositionDown:
                generatedCodeBuilder.Append(this.OperatorKeyword(prefix.Operator));
                generatedCodeBuilder.Append(' ');
                this.WriteExpression(prefix.Arguments[0], generatedCodeBuilder);
                break;
            case Operator.Either:
                generatedCodeBuilder.Append("EITHER ");
                this.WriteExpression(prefix.Arguments[0], generatedCodeBuilder);
                generatedCodeBuilder.Append(" OR ");
                this.WriteExpression(prefix.Arguments[1], generatedCodeBuilder);
                break;
            default:
                generatedCodeBuilder.Append(this.OperatorKeyword(prefix.Operator));
                generatedCodeBuilder.Append(' ');
                this.WriteExpression(prefix.Arguments[0], generatedCodeBuilder);
                generatedCodeBuilder.Append(" AND ");
                this.WriteExpression(prefix.Arguments[1], generatedCodeBuilder);
                break;
        }
    }

    /// <summary>
    /// Returns the Topsy Turvy keyword string for the given operator.
    /// </summary>
    /// <param name="theOperator">The operator to convert.</param>
    /// <returns>The keyword string.</returns>
    private string OperatorKeyword(Operator theOperator) => theOperator switch
    {
        Operator.Sum => "SUM OF",
        Operator.Difference => "DIFFERENCE OF",
        Operator.Product => "PRODUCT OF",
        Operator.Quotient => "QUOTIENT OF",
        Operator.Remainder => "REMAINDER OF",
        Operator.Larger => "LARGER OF",
        Operator.Smaller => "SMALLER OF",
        Operator.Both => "BOTH",
        Operator.Either => "EITHER",
        Operator.HardlyEver => "HARDLY EVER",
        Operator.Alike => "ALIKE",
        Operator.Unlike => "UNLIKE",
        Operator.PreAdamite => "PRE-ADAMITE",
        Operator.LowerDegree => "LOWER DEGREE",
        Operator.WovenOf => "WOVEN OF",
        Operator.Summon => "SUMMON",
        Operator.AllOf => "ALL OF",
        Operator.AnyOf => "ANY OF",
        Operator.ChordOf => "CHORD OF",
        Operator.HarmonyOf => "HARMONY OF",
        Operator.DiscordOf => "DISCORD OF",
        Operator.InversionOf => "INVERSION OF",
        Operator.TranspositionUp => "TRANSPOSITION UP",
        Operator.TranspositionDown => "TRANSPOSITION DOWN",
        _ => throw new ArgumentOutOfRangeException(nameof(theOperator), theOperator, "Unknown operator.")
    };

    /// <summary>
    /// Returns the Topsy Turvy type keyword for the given <see cref="LiteralType"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>The type keyword string.</returns>
    private string TypeKeyword(LiteralType type) => type switch
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
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown literal type.")
    };

    /// <summary>
    /// Formats a switch-case literal value as a Topsy Turvy source token.
    /// </summary>
    /// <param name="literal">The raw literal value from the AST.</param>
    /// <returns>A source-ready string representation of the literal.</returns>
    private string FormatSwitchLiteral(object? literal) => literal switch
    {
        string stringLiteral => $"\"{stringLiteral}\"",
        bool booleanLiteral => booleanLiteral
            ? Keywords.Literals.Verity
            : Keywords.Literals.Nay,
        null => Keywords.Literals.Naught,
        _ => Convert.ToString(literal, CultureInfo.InvariantCulture) ?? "0"
    };
}
