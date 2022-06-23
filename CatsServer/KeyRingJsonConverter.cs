using Net.Leksi.KeyBox;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatsServer;

public class KeyRingJsonConverter<T> : JsonConverter<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;

    public KeyRingJsonConverter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject)
        {
            T item = _serviceProvider.GetRequiredService<T>();
            Type itemType = item.GetType();
            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.EndObject)
                {
                    return item;
                }

                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }


                string? propertyName = reader.GetString();
                if (propertyName is null)
                {
                    throw new JsonException();
                }

                if(propertyName == "$key")
                {
                    if(!reader.Read())
                    {
                        throw new JsonException();
                    }
                    IKeyRing? keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(item);
                    if(keyRing is null || reader.TokenType != JsonTokenType.StartArray)
                    {
                        JsonSerializer.Deserialize<object>(ref reader, options);
                    }
                    else
                    {
                        for (int i = 0; reader.Read() && reader.TokenType is not JsonTokenType.EndArray; ++i)
                        {
                            keyRing[i] = JsonSerializer.Deserialize(ref reader, keyRing.GetPartType(i), options);
                        }
                    }
                }
                else
                {
                    if(itemType.GetProperty(propertyName) is PropertyInfo propertyInfo && propertyInfo.CanWrite)
                    {
                        propertyInfo.SetValue(item, JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options));
                    } 
                    else
                    {
                        JsonSerializer.Deserialize<object>(ref reader, options);
                    }
                }
            }
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$key");
            writer.WriteStartArray();
            IKeyRing keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(value);
            foreach (var item in keyRing.Values)
            {
                JsonSerializer.Serialize(writer, item, item?.GetType() ?? typeof(object), options);
            }
            writer.WriteEndArray();
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                writer.WritePropertyName(pi.Name);
                JsonSerializer.Serialize(writer, pi.GetValue(value), pi.PropertyType, options);
            }
            writer.WriteEndObject();
        }
    }
}
