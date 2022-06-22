using CatsModel;
using CatsModel.Filters;
using CatsServer;
using CatsUtil;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.KeyBox;
using System.Text;
using System.Text.Json;

namespace CatsServer.Controllers;

public class CatsController : Controller
{
    [Route("/breeds")]
    public async Task GetBreedsAsync()
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>());
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Breed>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetBreedsAsync(), jsonSerializerOptions);
    }

    [Route("/catteries")]
    public async Task GetCatteriesAsync()
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>());
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cattery>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetCatteriesAsync(), jsonSerializerOptions);
    }

    [Route("/cats/{filter?}")]
    public async Task GetCatsAsync(string? filter)
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>());
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<DateOnlyJsonConverter>());
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<EnumJsonConverter<Gender>>());
        CatListFilter? filterObject = null;
        if (filter is { }) {
            filterObject = JsonSerializer.Deserialize<CatListFilter>(filter, jsonSerializerOptions); 
        }
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cat>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetCatsAsync(filterObject), jsonSerializerOptions);
    }


}

