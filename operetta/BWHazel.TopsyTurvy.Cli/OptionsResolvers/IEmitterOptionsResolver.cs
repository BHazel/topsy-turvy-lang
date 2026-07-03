using System;
using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and executes a compiler toolchain emit operation for a given <c>--emit</c> or <c>--target</c> in the CLI.
/// </summary>
/// <typeparam name="TOptions">The options type this resolver builds and uses to configure the toolchain emit operation.</typeparam>
/// <remarks>
/// Every supported <c>--emit</c> or <c>--target</c> value has a single implementation of this interface.  For operations
/// which do not require any options, <typeparamref name="TOptions"/> should be <see cref="NoOptions"/>.
/// </remarks>
public interface IEmitterOptionsResolver<TOptions>
{
    /// <summary>
    /// The file extensions this resolver accepts.
    /// </summary>
    /// <returns>The allowed file extensions.</returns>
    IReadOnlyCollection<string> GetAllowedExtensions();

    /// <summary>
    /// Determines whether the filename has one of the specified file extensions.
    /// </summary>
    /// <param name="filename">The filename to check.</param>
    /// <returns><c>true</c> if the extension is supported, otherwise <c>false</c>.</returns>
    bool CanEmit(string filename) =>
        this.GetAllowedExtensions()
            .Any(extension => filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Applies the provided configuration on top of any base options returning the final combined options.
    /// </summary>
    /// <param name="baseOptions">The base options already determined from CLI arguments or defaults, before any configuration values from the command-line are applied.</param>
    /// <param name="config">The parsed configuration <c>key:value</c> pairs, or empty if none were given.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>The final <typeparamref name="TOptions"/> to configure the emit operation with.</returns>
    /// <remarks>
    /// An unrecognised key should be reported to the user but treated as non-fatal and otherwise ignored.
    /// </remarks>
    /// <exception cref="ToolchainConfigException">Thrown when the provided configuration contains an invalid value for a recognised key.</exception>
    TOptions Apply(TOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe);

    /// <summary>
    /// Reads the provided filename and performs the compiler emit operation, reporting any errors encountered.
    /// </summary>
    /// <param name="filename">The file to read and process.</param>
    /// <param name="options">The final options, as returned by <see cref="Apply"/>.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    int Emit(string filename, TOptions options, bool tiptoe);
}
