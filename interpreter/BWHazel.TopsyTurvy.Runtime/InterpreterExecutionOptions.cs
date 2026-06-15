using System;
using System.Threading;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Options for configuring the execution of the Topsy Turvy interpreter.
/// </summary>
/// <remarks>
/// <para>
/// Supported configuration options include:
/// </para>
/// <para>
/// ### Execution Timeout
/// An execution timeout can be configured to guard against infinite loops or excessively long execution times.  In addition to
/// setting the <see cref="ExecutionTimeout"/> property a <see cref="CancellationToken"/> should be passed from a
/// <see cref="CancellationTokenSource"/> on execution.
/// </para>
/// <para>
/// ### Source File Path
/// The source file path is an absolute or relative path of the source file being executed.  It is used to resolve the relative
/// import paths used in the <c>PRAY ADMIT</c> import statements.  This can be <c>null</c> when executing from a string with no
/// backing file.
/// </para>
/// <para>
/// ### Source File Resolver
/// This enables custom resolution of import paths, for example to support virtual file systems or to resolve imports from a database.
/// If this is not provided the interpreter will attempt to read files from the real file system.
/// </para>
/// </remarks>
/// <param name="ExecutionTimeout">A maximum wall-clock duration for execution, after which execution will be cancelled.</param>
/// <param name="SourceFilePath">The absolute or relative path of the source file being executed, used to resolve relative import paths.</param>
/// <param name="SourceFileResolver">An optional delegate that resolves an import filename to its source text, <c>null</c> to use the real file system.</param>
public record InterpreterExecutionOptions(
    TimeSpan? ExecutionTimeout,
    string? SourceFilePath,
    Func<string, string?>? SourceFileResolver);