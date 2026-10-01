using System.Text.Json;
using System.Text.Json.Serialization;

namespace DMS.Application.DTO.Circulars;

/// <summary>
/// Reads a list of strings sent either as a JSON array (["Master","Chief Engineer"])
/// or as one comma-separated string ("Master, Chief Engineer"). The circular form sends
/// the ranks as a string; without this the whole Edit request failed with 400 and nothing was saved.
/// Always writes a JSON array.
/// </summary>
public sealed class StringOrListConverter : JsonConverter<List<string>>
{
    // Also called for null, so the property becomes an empty list instead of null.
    public override bool HandleNull => true;

    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new List<string>();

            case JsonTokenType.String:
                return Split(reader.GetString());

            case JsonTokenType.StartArray:
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String)
                        list.AddRange(Split(reader.GetString()));
                    else if (reader.TokenType != JsonTokenType.Null)
                        throw new JsonException("Expected text in the list.");
                }
                return list;

            default:
                throw new JsonException("Expected a list of text or comma-separated text.");
        }
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value ?? new List<string>())
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }

    private static List<string> Split(string? text) =>
        (text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
