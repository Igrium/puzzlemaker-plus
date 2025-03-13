using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
    }

    public static JsonConverter<T> GetConverter<T>(this JsonSerializerOptions options)
    {
        return (JsonConverter<T>)options.GetConverter(typeof(T));
    }
}

/// <summary>
/// Serializes a json array of values, but if a single value is provided instead, it returns a list with one value.
/// Doesn't work if value is serialized as json array!
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public class ListOrValueConverter<T> : JsonConverter<IList<T>>
{
    public override IList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueConverter = options.GetConverter<T>();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                // TODO: is throwing on null the correct approach?
                list.Add(valueConverter.Read(ref reader, typeof(T), options) ?? throw new JsonException());
            }
            return list;
        }
        else
        {
            var list = new List<T>();
            list.Add(valueConverter.Read(ref reader, typeof(T), options) ?? throw new JsonException());
            return list;
        }
    }

    public override void Write(Utf8JsonWriter writer, IList<T> value, JsonSerializerOptions options)
    {
        var valueConverter = options.GetConverter<T>();
        if (value.Count == 1)
        {
            valueConverter.Write(writer, value[0], options);
        }
        else
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                valueConverter.Write(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}

/// <summary>
/// Serializes a dictionary of string keys, with the special stipulation that a dict with a single value "default" can pe represented as the value on its own.
/// Doesn't work if value is serialized as json object!
/// </summary>
/// <typeparam name="T"></typeparam>
public class DictOrValueConverter<T> : JsonConverter<IDictionary<string, T>>
{
    private readonly string _defaultName;

    public DictOrValueConverter(string defaultName)
    {
        _defaultName = defaultName;
    }

    public DictOrValueConverter()
    {
        _defaultName = "default";
    }

    public override IDictionary<string, T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueConverter = options.GetConverter<T>();
        Dictionary<string, T> dict = new();

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                // Key
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string propName = reader.GetString() ?? throw new JsonException();

                reader.Read();
                T val = valueConverter.Read(ref reader, typeof(T), options)!;

                dict[propName] = val;
            }
            return dict;
        }
        else
        {
            dict[_defaultName] = valueConverter.Read(ref reader, typeof(T), options)!;
            return dict;
        }
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, T> value, JsonSerializerOptions options)
    {
        var valueConverter = options.GetConverter<T>();
        if (value.Count() == 1 && value.TryGetValue(_defaultName, out T? single))
        {
            valueConverter.Write(writer, single, options);
        }
        else
        {
            writer.WriteStartObject();
            foreach (var (k, v) in value)
            {
                writer.WritePropertyName(k);
                valueConverter.Write(writer, v, options);
            }
            writer.WriteEndObject();
        }
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