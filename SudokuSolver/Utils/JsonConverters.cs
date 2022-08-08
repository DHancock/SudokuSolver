namespace Sudoku.Utils;

internal class WINDOWPLACEMENTConverter : JsonConverter<WINDOWPLACEMENT>
{
    public override WINDOWPLACEMENT Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        WINDOWPLACEMENT placement = default;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case nameof(placement.length): placement.length = Utils.ReadUInt(ref reader); break;
                    case nameof(placement.flags): placement.flags = (WINDOWPLACEMENT_FLAGS)Utils.ReadUInt(ref reader); break;
                    case nameof(placement.showCmd): placement.showCmd = (SHOW_WINDOW_CMD)Utils.ReadUInt(ref reader); break;

                    case nameof(placement.ptMinPosition):
                        {
                            placement.ptMinPosition = JsonSerializer.Deserialize<POINT>(ref reader, options);
                            break;
                        }
                    case nameof(placement.ptMaxPosition):
                        {
                            placement.ptMaxPosition = JsonSerializer.Deserialize<POINT>(ref reader, options);
                            break;
                        }
                    case nameof(placement.rcNormalPosition):
                        {
                            placement.rcNormalPosition = JsonSerializer.Deserialize<RECT>(ref reader, options);
                            break;
                        }
                    default:
                        {
                            reader.Skip();
                            break;
                        }
                }

            }
        }

        return placement;
    }

    public override void Write(Utf8JsonWriter writer, WINDOWPLACEMENT value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(value.length), value.length);
        writer.WriteNumber(nameof(value.flags), (uint)value.flags);
        writer.WriteNumber(nameof(value.showCmd), (uint)value.showCmd);

        WriteObject(nameof(value.ptMaxPosition), value.ptMaxPosition, writer, options);
        WriteObject(nameof(value.ptMinPosition), value.ptMinPosition, writer, options);
        WriteObject(nameof(value.rcNormalPosition), value.rcNormalPosition, writer, options);
        writer.WriteEndObject();
    }

    private static void WriteObject<T>(string name, T value, Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartObject(name);
        JsonSerializer.Serialize<T>(writer, value, options);
        writer.WriteEndObject();
    }
}


internal class POINTConverter : JsonConverter<POINT>
{
    public override POINT Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        POINT point = default;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case nameof(point.x): point.x = Utils.ReadInt(ref reader); break;
                    case nameof(point.y): point.y = Utils.ReadInt(ref reader); break;
                    default:
                        throw new JsonException(); 
                }
            }
        }

        return point;
    }

    public override void Write(Utf8JsonWriter writer, POINT value, JsonSerializerOptions options)
    {
        writer.WriteNumber(nameof(POINT.x), value.x);
        writer.WriteNumber(nameof(POINT.y), value.y);
    }
}


internal class RECTConverter : JsonConverter<RECT>
{
    public override RECT Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        RECT rect = default;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case nameof(rect.left): rect.left = Utils.ReadInt(ref reader); break;
                    case nameof(rect.top): rect.top = Utils.ReadInt(ref reader); break;
                    case nameof(rect.right): rect.right = Utils.ReadInt(ref reader); break;
                    case nameof(rect.bottom): rect.bottom = Utils.ReadInt(ref reader); break;
                    default:
                        throw new JsonException();
                }
            }
        }

        return rect;
    }

    public override void Write(Utf8JsonWriter writer, RECT value, JsonSerializerOptions options)
    {
        writer.WriteNumber(nameof(RECT.left), value.left);
        writer.WriteNumber(nameof(RECT.top), value.top);
        writer.WriteNumber(nameof(RECT.right), value.right);
        writer.WriteNumber(nameof(RECT.bottom), value.bottom);
    }
}


internal static class Utils
{
    public static int ReadInt(ref Utf8JsonReader reader)
    {
        reader.Read();
        return reader.GetInt32();
    }

    public static uint ReadUInt(ref Utf8JsonReader reader)
    {
        reader.Read();
        return reader.GetUInt32();
    }
}



