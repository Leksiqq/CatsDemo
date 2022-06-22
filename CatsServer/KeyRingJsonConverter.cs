using Net.Leksi.KeyBox;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatsServer;

public class KeyRingJsonConverter<T> : JsonConverter<T>
{
    private readonly IServiceProvider _serviceProvider;

    public KeyRingJsonConverter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if(value is null)
        {
            writer.WriteNullValue();
        } else
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$key");
            writer.WriteStartArray();
            IKeyRing keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(value);
            foreach(var item in keyRing.Values)
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
