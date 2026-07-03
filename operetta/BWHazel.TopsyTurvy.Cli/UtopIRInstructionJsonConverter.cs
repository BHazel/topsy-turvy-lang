using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// A JSON converter for the UtopIR AST <see cref="UtopIRInstruction"/> type and all derived types.
/// </summary>
/// <remarks>
/// Serialises instructions polymorphically, adding a <c>$type</c> discriminator field
/// containing the runtime class name.
/// </remarks>
public class UtopIRInstructionJsonConverter : JsonConverter<UtopIRInstruction>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeof(UtopIRInstruction).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override UtopIRInstruction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("UtopIR AST deserialisation is not supported in the Topsy Turvy CLI.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, UtopIRInstruction value, JsonSerializerOptions options)
    {
        Type runtimeType = value.GetType();
        writer.WriteStartObject();
        writer.WriteString("$type", runtimeType.Name);

        foreach (PropertyInfo property in runtimeType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            writer.WritePropertyName(propertyName);
            JsonSerializer.Serialize(writer, property.GetValue(value), property.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}
