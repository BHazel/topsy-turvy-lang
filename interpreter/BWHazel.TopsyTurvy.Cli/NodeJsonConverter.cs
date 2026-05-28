using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// A JSON converter for the Topsy Turvy AST <see cref="Node"/> type and all derived types.
/// </summary>
/// <remarks>
/// Serialises nodes polymorphically adding a <c>$type</c> discriminator field containing the
/// runtime class name.
/// </remarks>
public class NodeJsonConverter : JsonConverter<Node>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeof(Node).IsAssignableFrom(typeToConvert);
    
    /// <inheritdoc />
    public override Node Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("AST deserialisation is not supported in the Topsy Turvy CLI.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
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
