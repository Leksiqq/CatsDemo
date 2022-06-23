using Net.Leksi.KeyBox;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatsServer;

public class KeyRingJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public KeyRingJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public override bool CanConvert(Type typeToConvert)
    {
        IKeyBox keyBox = _serviceProvider.GetRequiredService<IKeyBox>();
        return keyBox.HasMappedKeys(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(KeyRingJsonConverter<>).MakeGenericType(new Type[] { typeToConvert });
        return (JsonConverter)_serviceProvider.GetRequiredService(converterType);
    }
}
