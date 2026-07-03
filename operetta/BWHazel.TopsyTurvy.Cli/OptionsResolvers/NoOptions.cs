namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Satisfies <c>TOptions</c> for an <see cref="IEmitterOptionsResolver{TOptions}"/> whose emit
/// format or target has no configurable settings at all.
/// </summary>
public sealed record NoOptions
{
    /// <summary>
    /// The single, reusable instance which carries no data.
    /// </summary>
    public static readonly NoOptions Default = new();
}
