using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI director (debug) command.
/// </summary>
/// <remarks>
/// All tests pipe debug console commands via standard input and use <c>--tiptoe</c> so output is plain text,
/// making stdout assertions straightforward.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class DirectorCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    private const string SimpleSource =
        """
        HARK! "Test"
        PRAY WELCOME result AS A PEER BEING 0
        result IS APPOINTED 42
        BEHOLD result
        FINALE.
        """;

    /// <summary>
    /// Tests that the director command pauses at a breakpoint, reports the current variables, steps forward and
    /// runs the remainder of the programme to completion with exit code 0.
    /// </summary>
    [Fact]
    public async Task Director_WithBreakpointStepAndProceed_CompletesWithExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int exitCode, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "mark 3\nproceed\narmoury\nstep\narmoury\nproceed\n");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("Breakpoint set at line 3");
        stdout.ShouldContain("Paused (BreakpointHit) at line 3");
        stdout.ShouldContain("result: PEER = 0");
        stdout.ShouldContain("Paused (StepComplete) at line 4");
        stdout.ShouldContain("result: PEER = 42");
        stdout.ShouldContain("42");
    }

    /// <summary>
    /// Tests that the cue command prints the current line and function name of the selected frame.
    /// </summary>
    [Fact]
    public async Task Director_WithCueCommand_PrintsCurrentLineAndFunctionName()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "mark 3\nproceed\ncue\nproceed\n");

        stdout.ShouldContain("Line 3 in <programme>.");
    }

    /// <summary>
    /// Tests that the words command prints the source text of the current line of the selected frame.
    /// </summary>
    [Fact]
    public async Task Director_WithWordsCommand_PrintsSourceLineText()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "mark 3\nproceed\nwords\nproceed\n");

        stdout.ShouldContain("3: result IS APPOINTED 42");
    }

    /// <summary>
    /// Tests that the director command reports a breakpoint on a line with no statement as unreachable.
    /// </summary>
    [Fact]
    public async Task Director_WithBreakpointOnBlankLine_ReportsUnreachable()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "mark 100\nfinale\n");

        stdout.ShouldContain("unreachable");
    }

    /// <summary>
    /// Tests that the director command accepts the debug alias.
    /// </summary>
    [Fact]
    public async Task Director_UsingDebugAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int exitCode, string _, string _) = await this.RunWithStdinAsync("debug prog.topsy --tiptoe", "finale\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the finale command ends the session cleanly without reporting a spurious execution error.
    /// </summary>
    [Fact]
    public async Task Director_WithFinaleWhilePaused_EndsCleanlyWithNoErrorOutput()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int exitCode, string stdout, string stderr) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "mark 3\nproceed\nfinale\n");

        exitCode.ShouldBe(0);
        (stdout + stderr).ShouldNotContain("timed out");
    }

    /// <summary>
    /// Tests that the director command reports a clean error for a file that does not exist.
    /// </summary>
    [Fact]
    public async Task Director_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string stderr) = await this.RunWithStdinAsync(
            "director missing.topsy --tiptoe",
            "finale\n");

        exitCode.ShouldBe(1);
        stderr.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that the entracte command writes the debug console command list to stdout.
    /// </summary>
    [Fact]
    public async Task Director_WithEntracteCommand_WritesHelpToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SimpleSource);

        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "entracte\nfinale\n");

        stdout.ShouldContain("proceed");
        stdout.ShouldContain("finale");
    }

    /// <summary>
    /// Tests that the director command pauses with an unhandled-error reason instead of ending immediately when a
    /// file raises a runtime error with no breakpoints set, then reports the error normally once resumed.
    /// </summary>
    [Fact]
    public async Task Director_WithNoBreakpointsAndRuntimeError_PausesThenReportsErrorOnResume()
    {
        const string errorSource =
            """
            HARK! "Error"
            A HIDEOUS CURSE ON "boom"
            FINALE.
            """;

        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), errorSource);

        (int exitCode, string stdout, string _) = await this.RunWithStdinAsync(
            "director prog.topsy --tiptoe",
            "proceed\ntroupe\nproceed\n");

        exitCode.ShouldBe(1);
        stdout.ShouldContain("Paused (UnhandledError)");
        stdout.ShouldContain("boom");
    }

    /// <summary>
    /// Tests that <c>director --adapter</c> completes a basic DAP <c>initialize</c>, <c>launch</c> and
    /// <c>setBreakpoints</c> handshake over stdio, then exits cleanly on <c>disconnect</c>.
    /// </summary>
    [Fact]
    public async Task DirectorAdapter_WithInitializeLaunchAndSetBreakpoints_CompletesHandshakeAndExitsCleanly()
    {
        string filePath = Path.Combine(this.WorkingDirectory, "prog.topsy");
        File.WriteAllText(filePath, SimpleSource);

        ProcessStartInfo startInfo = new(this.BinaryPath, "director --adapter")
        {
            WorkingDirectory = this.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 1,
            ["type"] = "request",
            ["command"] = "initialize",
            ["arguments"] = new JsonObject()
            {
                ["adapterID"] = "topsy-turvy"
            }
        });

        JsonObject initializeResponse = await ReadDapResponseAsync(process.StandardOutput, "initialize", NewStepTimeout());
        initializeResponse["success"]!.GetValue<bool>().ShouldBeTrue();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 2,
            ["type"] = "request",
            ["command"] = "launch",
            ["arguments"] = new JsonObject()
            {
                ["program"] = filePath
            }
        });

        JsonObject launchResponse = await ReadDapResponseAsync(process.StandardOutput, "launch", NewStepTimeout());
        launchResponse["success"]!.GetValue<bool>().ShouldBeTrue();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 3,
            ["type"] = "request",
            ["command"] = "setBreakpoints",
            ["arguments"] = new JsonObject()
            {
                ["source"] = new JsonObject()
                {
                    ["path"] = filePath
                },
                ["breakpoints"] = new JsonArray(new JsonObject()
                {
                    ["line"] = 3
                })
            }
        });

        JsonObject setBreakpointsResponse = await ReadDapResponseAsync(process.StandardOutput, "setBreakpoints", NewStepTimeout());
        setBreakpointsResponse["success"]!.GetValue<bool>().ShouldBeTrue();
        JsonArray breakpoints = setBreakpointsResponse["body"]!["breakpoints"]!.AsArray();
        breakpoints.Count.ShouldBe(1);
        breakpoints[0]!["verified"]!.GetValue<bool>().ShouldBeTrue();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 4,
            ["type"] = "request",
            ["command"] = "disconnect",
            ["arguments"] = new JsonObject()
        });

        JsonObject disconnectResponse = await ReadDapResponseAsync(process.StandardOutput, "disconnect", NewStepTimeout());
        disconnectResponse["success"]!.GetValue<bool>().ShouldBeTrue();

        process.StandardInput.Close();
        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        exited.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>director --adapter</c> runs the target programme end to end, reporting <c>stopped</c>,
    /// <c>output</c> and <c>terminated</c> events at the right points.
    /// </summary>
    [Fact]
    public async Task DirectorAdapter_WithConfigurationDoneAndContinue_RunsProgrammeAndReportsEvents()
    {
        string filePath = Path.Combine(this.WorkingDirectory, "prog.topsy");
        File.WriteAllText(filePath, SimpleSource);

        ProcessStartInfo startInfo = new(this.BinaryPath, "director --adapter")
        {
            WorkingDirectory = this.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject
        {
            ["seq"] = 1,
            ["type"] = "request",
            ["command"] = "initialize",
            ["arguments"] = new JsonObject { ["adapterID"] = "topsy-turvy" }
        });

        await ReadDapResponseAsync(process.StandardOutput, "initialize", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject
        {
            ["seq"] = 2,
            ["type"] = "request",
            ["command"] = "launch",
            ["arguments"] = new JsonObject { ["program"] = filePath }
        });

        await ReadDapEventAsync(process.StandardOutput, "initialized", NewStepTimeout());
        await ReadDapResponseAsync(process.StandardOutput, "launch", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 3,
            ["type"] = "request",
            ["command"] = "setBreakpoints",
            ["arguments"] = new JsonObject()
            {
                ["source"] = new JsonObject()
                {
                    ["path"] = filePath
                },
                ["breakpoints"] = new JsonArray(new JsonObject()
                {
                    ["line"] = 3
                })
            }
        });

        await ReadDapResponseAsync(process.StandardOutput, "setBreakpoints", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 4,
            ["type"] = "request",
            ["command"] = "configurationDone",
            ["arguments"] = new JsonObject()
        });

        await ReadDapResponseAsync(process.StandardOutput, "configurationDone", NewStepTimeout());

        JsonObject stoppedEvent = await ReadDapEventAsync(process.StandardOutput, "stopped", NewStepTimeout());
        stoppedEvent["body"]!["reason"]!.GetValue<string>().ShouldBe("breakpoint");

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 5,
            ["type"] = "request",
            ["command"] = "stackTrace",
            ["arguments"] = new JsonObject()
            {
                ["threadId"] = 1
            }
        });

        JsonObject stackTraceResponse = await ReadDapResponseAsync(process.StandardOutput, "stackTrace", NewStepTimeout());
        JsonArray stackFrames = stackTraceResponse["body"]!["stackFrames"]!.AsArray();
        stackFrames.Count.ShouldBe(1);
        stackFrames[0]!["source"]!["path"]!.GetValue<string>().ShouldBe(filePath);

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 6,
            ["type"] = "request",
            ["command"] = "continue",
            ["arguments"] = new JsonObject()
            {
                ["threadId"] = 1
            }
        });

        await ReadDapResponseAsync(process.StandardOutput, "continue", NewStepTimeout());

        JsonObject outputEvent = await ReadDapEventAsync(process.StandardOutput, "output", NewStepTimeout());
        outputEvent["body"]!["output"]!.GetValue<string>().ShouldContain("42");

        await ReadDapEventAsync(process.StandardOutput, "terminated", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 7,
            ["type"] = "request",
            ["command"] = "disconnect",
            ["arguments"] = new JsonObject()
        });

        await ReadDapResponseAsync(process.StandardOutput, "disconnect", NewStepTimeout());

        process.StandardInput.Close();
        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        exited.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>director --adapter</c> reports an unhandled runtime error as a standard error <c>output</c>
    /// event followed by an <c>exited</c> event with a non-zero exit code.
    /// </summary>
    [Fact]
    public async Task DirectorAdapter_WithNoBreakpointsAndRuntimeError_ReportsErrorAndNonZeroExitCode()
    {
        const string errorSource =
            """
            HARK! "Error"
            A HIDEOUS CURSE ON "boom"
            FINALE.
            """;

        string filePath = Path.Combine(this.WorkingDirectory, "prog.topsy");
        File.WriteAllText(filePath, errorSource);

        ProcessStartInfo startInfo = new(this.BinaryPath, "director --adapter")
        {
            WorkingDirectory = this.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 1,
            ["type"] = "request",
            ["command"] = "initialize",
            ["arguments"] = new JsonObject { ["adapterID"] = "topsy-turvy" }
        });

        await ReadDapResponseAsync(process.StandardOutput, "initialize", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 2,
            ["type"] = "request",
            ["command"] = "launch",
            ["arguments"] = new JsonObject { ["program"] = filePath }
        });

        await ReadDapEventAsync(process.StandardOutput, "initialized", NewStepTimeout());
        await ReadDapResponseAsync(process.StandardOutput, "launch", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 3,
            ["type"] = "request",
            ["command"] = "configurationDone",
            ["arguments"] = new JsonObject()
        });

        await ReadDapResponseAsync(process.StandardOutput, "configurationDone", NewStepTimeout());

        JsonObject stoppedEvent = await ReadDapEventAsync(process.StandardOutput, "stopped", NewStepTimeout());
        stoppedEvent["body"]!["reason"]!.GetValue<string>().ShouldBe("exception");

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 4,
            ["type"] = "request",
            ["command"] = "continue",
            ["arguments"] = new JsonObject { ["threadId"] = 1 }
        });

        await ReadDapResponseAsync(process.StandardOutput, "continue", NewStepTimeout());

        JsonObject errorOutputEvent = await ReadDapEventAsync(process.StandardOutput, "output", NewStepTimeout());
        errorOutputEvent["body"]!["category"]!.GetValue<string>().ShouldBe("stderr");
        errorOutputEvent["body"]!["output"]!.GetValue<string>().ShouldContain("boom");

        JsonObject exitedEvent = await ReadDapEventAsync(process.StandardOutput, "exited", NewStepTimeout());
        exitedEvent["body"]!["exitCode"]!.GetValue<int>().ShouldBe(1);

        await ReadDapEventAsync(process.StandardOutput, "terminated", NewStepTimeout());

        await WriteDapMessageAsync(process.StandardInput, new JsonObject()
        {
            ["seq"] = 5,
            ["type"] = "request",
            ["command"] = "disconnect",
            ["arguments"] = new JsonObject()
        });

        await ReadDapResponseAsync(process.StandardOutput, "disconnect", NewStepTimeout());

        process.StandardInput.Close();
        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        exited.ShouldBeTrue();
    }

    /// <summary>
    /// DAP messages read ahead of where the test currently needs them.
    /// </summary>
    /// <remarks>
    /// For example, an event that arrived while waiting for an unrelated response. Kept so a later
    /// <see cref="ReadDapEventAsync"/> or <see cref="ReadDapResponseAsync"/> call can still find them instead of the
    /// message being silently dropped.
    /// </remarks>
    private readonly List<JsonObject> pendingDapMessages = [];

    /// <summary>
    /// Creates a fresh 10-second timeout for a single DAP request/response round trip, rather than sharing one
    /// budget across the whole handshake, since a cold .NET process start can itself take several seconds.
    /// </summary>
    /// <returns>A token that cancels in 10 seconds.</returns>
    private static CancellationToken NewStepTimeout() => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    /// <summary>
    /// Writes a single DAP message to <paramref name="writer"/> using the Content-Length header framing the
    /// protocol requires.
    /// </summary>
    /// <param name="writer">The process standard input stream.</param>
    /// <param name="message">The message body to send.</param>
    private static async Task WriteDapMessageAsync(StreamWriter writer, JsonObject message)
    {
        string body = message.ToJsonString();
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string header = $"Content-Length: {bodyBytes.Length}\r\n\r\n";

        await writer.BaseStream.WriteAsync(Encoding.ASCII.GetBytes(header));
        await writer.BaseStream.WriteAsync(bodyBytes);
        await writer.BaseStream.FlushAsync();
    }

    /// <summary>
    /// Waits for a <c>response</c> message for <paramref name="command"/>, per <see cref="ReadDapMessageAsync"/>.
    /// </summary>
    /// <param name="reader">The process standard output stream.</param>
    /// <param name="command">The request command whose response is awaited.</param>
    /// <param name="cancellationToken">A token that stops waiting if the adapter never responds.</param>
    /// <returns>The parsed response message.</returns>
    private Task<JsonObject> ReadDapResponseAsync(StreamReader reader, string command, CancellationToken cancellationToken) =>
        this.ReadDapMessageAsync(
            reader,
            message => message["type"]?.GetValue<string>() == "response" && message["command"]?.GetValue<string>() == command,
            cancellationToken);

    /// <summary>
    /// Waits for an <c>event</c> message named <paramref name="eventName"/>, per <see cref="ReadDapMessageAsync"/>.
    /// </summary>
    /// <param name="reader">The process standard output stream.</param>
    /// <param name="eventName">The event name to wait for.</param>
    /// <param name="cancellationToken">A token that stops waiting if the adapter never sends it.</param>
    /// <returns>The parsed event message.</returns>
    private Task<JsonObject> ReadDapEventAsync(StreamReader reader, string eventName, CancellationToken cancellationToken) =>
        this.ReadDapMessageAsync(
            reader,
            message => message["type"]?.GetValue<string>() == "event" && message["event"]?.GetValue<string>() == eventName,
            cancellationToken);

    /// <summary>
    /// Reads Content-Length-framed DAP messages from <paramref name="reader"/> until one matching
    /// <paramref name="predicate"/> is found.
    /// </summary>
    /// <remarks>
    /// Checks <see cref="pendingDapMessages"/> first for one already read ahead. Every other message read along
    /// the way is kept there rather than discarded, since a response and an event this host sends can otherwise
    /// interleave in either order.
    /// </remarks>
    /// <param name="reader">The process standard output stream.</param>
    /// <param name="predicate">A predicate to identify the desired message.</param>
    /// <param name="cancellationToken">A token that stops waiting if no matching message ever arrives.</param>
    /// <returns>The matching message.</returns>
    private async Task<JsonObject> ReadDapMessageAsync(StreamReader reader, Func<JsonObject, bool> predicate, CancellationToken cancellationToken)
    {
        int pendingIndex = this.pendingDapMessages.FindIndex(message => predicate(message));
        if (pendingIndex >= 0)
        {
            JsonObject foundMessage = this.pendingDapMessages[pendingIndex];
            this.pendingDapMessages.RemoveAt(pendingIndex);
            return foundMessage;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? headerLine;
            int contentLength = 0;
            while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync(cancellationToken)))
            {
                if (headerLine.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    contentLength = int.Parse(headerLine.Split(':')[1].Trim());
                }
            }

            char[] bodyBuffer = new char[contentLength];
            int totalCharsRead = 0;
            while (totalCharsRead < contentLength)
            {
                int charsReadThisCall = await reader.ReadAsync(bodyBuffer.AsMemory(totalCharsRead, contentLength - totalCharsRead), cancellationToken);
                totalCharsRead += charsReadThisCall;
            }

            JsonObject message = JsonNode.Parse(new string(bodyBuffer))!.AsObject();
            if (predicate(message))
            {
                return message;
            }

            this.pendingDapMessages.Add(message);
        }
    }
}
