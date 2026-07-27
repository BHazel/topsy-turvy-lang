using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="SignatureHelpHandler"/> class.
/// </summary>
public class SignatureHelpHandlerTests : LanguageServerTestBase
{
    private readonly string functionSource = """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
          AND SO I FIND SUM OF lhs AND rhs
        MY DUTY IS DISCHARGED.
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        SignatureHelpHandler handler = new(manager);

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns null when no SUMMON keyword appears before the cursor.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoSummonOnLine_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.functionSource);
        SignatureHelpHandler handler = new(manager);

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: 0, character: 5), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns null when the call is already closed with "IF YOU PLEASE.".
    /// </summary>
    [Fact]
    public async Task Handle_WithClosedSummonCall_ReturnsNull()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
              AND SO I FIND SUM OF lhs AND rhs
            MY DUTY IS DISCHARGED.
            SUMMON add WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        SignatureHelpHandler handler = new(manager);

        string[] lines = source.Split('\n');
        int summonLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));
        int afterClose = lines[summonLine].Length;
        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: summonLine, character: afterClose), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns signature information when the cursor is inside a SUMMON call.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorInsideSummonCall_ReturnsSignatureInfo()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
              AND SO I FIND SUM OF lhs AND rhs
            MY DUTY IS DISCHARGED.
            SUMMON add WITH 1
            FINALE.
            """;

        DocumentStateManager manager = new();
        manager.Update(this.testUri, this.functionSource, this.parser.TryParse(this.functionSource));
        manager.Update(this.testUri, source, this.parser.TryParse(source));
        SignatureHelpHandler handler = new(manager);
        string[] lines = source.Split('\n');
        int summonLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));
        int cursorChar = lines[summonLine].Length;

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: summonLine, character: cursorChar), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Signatures.ShouldNotBeEmpty();
        result.Signatures.First().Label.Split('(')[0].ShouldBe("add");
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method advances the active parameter index for each AND token encountered.
    /// </summary>
    [Fact]
    public async Task Handle_WithAndTokenBeforeCursor_AdvancesActiveParameterIndex()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
              AND SO I FIND SUM OF lhs AND rhs
            MY DUTY IS DISCHARGED.
            SUMMON add WITH 1 AND
            FINALE.
            """;

        DocumentStateManager manager = new();
        manager.Update(this.testUri, this.functionSource, this.parser.TryParse(this.functionSource));
        manager.Update(this.testUri, source, this.parser.TryParse(source));
        SignatureHelpHandler handler = new(manager);
        string[] lines = source.Split('\n');
        int summonLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));
        int cursorChar = lines[summonLine].Length;

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: summonLine, character: cursorChar), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ActiveParameter.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns one <see cref="SignatureInformation"/>
    /// per overload when the called function name has more than one declared signature.
    /// </summary>
    [Fact]
    public async Task Handle_WithOverloadedFunction_ReturnsAllOverloadSignatures()
    {
        string overloadedSource = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF first AS A PEER AND second AS A PEER TO FIND YARN
              AND SO I FIND "two numbers"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF first AS A PEER AND second AS A PEER TO FIND YARN
              AND SO I FIND "two numbers"
            MY DUTY IS DISCHARGED.
            SUMMON describe WITH 1
            FINALE.
            """;

        DocumentStateManager manager = new();
        manager.Update(this.testUri, overloadedSource, this.parser.TryParse(overloadedSource));
        manager.Update(this.testUri, source, this.parser.TryParse(source));
        SignatureHelpHandler handler = new(manager);
        string[] lines = source.Split('\n');
        int summonLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));
        int cursorChar = lines[summonLine].Length;

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: summonLine, character: cursorChar), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Signatures.Count().ShouldBe(2);
        result.Signatures.ShouldAllBe(signature => signature.Label.Split('(')[0] == "describe");
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method includes each parameter type in an
    /// overload label, so two overloads differing only by parameter type render as distinct signatures.
    /// </summary>
    [Fact]
    public async Task Handle_WithOverloadsDifferingOnlyByParameterType_ReturnsDistinctLabels()
    {
        string overloadedSource = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.
            SUMMON describe WITH 1
            FINALE.
            """;

        DocumentStateManager manager = new();
        manager.Update(this.testUri, overloadedSource, this.parser.TryParse(overloadedSource));
        manager.Update(this.testUri, source, this.parser.TryParse(source));
        SignatureHelpHandler handler = new(manager);
        string[] lines = source.Split('\n');
        int summonLine = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));
        int cursorChar = lines[summonLine].Length;

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: summonLine, character: cursorChar), CancellationToken.None);

        result.ShouldNotBeNull();
        List<string> labels = [.. result.Signatures.Select(signature => signature.Label)];
        labels.Distinct().Count().ShouldBe(2);
        labels.ShouldContain(label => label.Contains("PEER"));
        labels.ShouldContain(label => label.Contains("YARN"));
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpHandler.Handle"/> method returns null when the line index is beyond the document length.
    /// </summary>
    [Fact]
    public async Task Handle_WithLineIndexBeyondDocument_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.functionSource);
        SignatureHelpHandler handler = new(manager);

        SignatureHelp? result = await handler.Handle(this.MakeRequest(line: 999, character: 0), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Creates a <see cref="SignatureHelpParams"/> object for the given line and character position.
    /// </summary>
    /// <param name="line">The line number of the position.</param>
    /// <param name="character">The character offset of the position.</param>
    /// <returns>A <see cref="SignatureHelpParams"/> for the test document at the specified position.</returns>
    private SignatureHelpParams MakeRequest(int line, int character) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character)
        };
}
