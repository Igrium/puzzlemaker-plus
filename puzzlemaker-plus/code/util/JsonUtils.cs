using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace PuzzlemakerPlus;

public static class JsonUtils
{
    /// <summary>
    /// Json serializer options with converters added for godot classes.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions();

    static JsonUtils()
    {
        JsonOptions.Converters.Add(new Vector3JsonConverter());
        JsonOptions.Converters.Add(new Vector3IJsonConverter());
        JsonOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        JsonOptions.PropertyNameCaseInsensitive = true;
    }

    public static JsonConverter<T> GetConverter<T>(this JsonSerializerOptions options)
    {
        return (JsonConverter<T>)options.GetConverter(typeof(T));
    }
}

public class DictOrValueJsonConverter<T> : JsonConverter<Dictionary<string, T>>
{
    public string DefaultName { get; set; } = "default";

    public override Dictionary<string, T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return options.GetConverter<Dictionary<string, T>>().Read(ref reader, typeToConvert, options);
        }
        else
        {
            var converter = options.GetConverter<T>();
            Dictionary<string, T> dict = new();
            var val = converter.Read(ref reader, typeof(T), options);
            if (val != null)
                dict[DefaultName] = val;
            return dict;
        }
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, T> value, JsonSerializerOptions options)
    {
        options.GetConverter<Dictionary<string, T>>().Write(writer, value, options);
    }
}

public class Vector3JsonConverter : JsonConverter<Vector3>
{

    internal static Vector3 ReadVecFromJson(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {

        float GetNumber(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return (float)reader.GetDouble();
                case JsonTokenType.String:
                    return float.Parse(reader.GetString() ?? "");
                default:
                    throw new JsonException("Invalid token type for vector element: " + reader.TokenType);
            }
        }

        Vector3 vec = new Vector3();
        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
                int i = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    vec[i] = GetNumber(ref reader);
                    i++;
                }
                return vec;
            case JsonTokenType.StartObject:
                while (reader.Read())
                {
                    // Key
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException();

                    string propertyName = reader.GetString()?.ToLower()?.Trim() ?? throw new JsonException();

                    // Value
                    reader.Read();
                    float val = GetNumber(ref reader);

                    if (propertyName == "x")
                        vec.X = val;
                    else if (propertyName == "y")
                        vec.Y = val;
                    else if (propertyName == "z")
                        vec.Z = val;
                }

                return vec;
            case JsonTokenType.String:
                string value = reader.GetString() ?? throw new JsonException();

                if (value.StartsWith('[') || value.StartsWith('{'))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                string[] split = value.Split(',', ' ');

                for (int f = 0; f < split.Length; f++)
                {
                    vec[f] = float.Parse(split[f]);
                }
                return vec;
            default:
                throw new JsonException("Invalid token type for Vector3: " + reader.TokenType);
        }
    }

    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadVecFromJson(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);

        writer.WriteEndArray();
    }
}

public class Vector3IJsonConverter : JsonConverter<Vector3I>
{
    public override Vector3I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Vector3JsonConverter.ReadVecFromJson(ref reader, typeToConvert, options).RoundInt();
    }

    public override void Write(Utf8JsonWriter writer, Vector3I value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);

        writer.WriteEndArray();
    }
}

public class DirectionJsonConverter : JsonConverter<Direction>
{
    public override Direction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString() ?? throw new JsonException();
            Direction direction;

            if (Directions.TryParseAxisString(str, out direction))
                return direction;
            else if (Enum.TryParse(str, true, out direction))
                return direction;
            else
                throw new JsonException("Unknown direction: " + str);
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            int num = reader.GetInt32();
            return (Direction)num;
        }
        else
        {
            throw new JsonException("Invalid token type for direction: " + reader.TokenType);
        }
    }

    public override void Write(Utf8JsonWriter writer, Direction value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetAxisString());
    }
}