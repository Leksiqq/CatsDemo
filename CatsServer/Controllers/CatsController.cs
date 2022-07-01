using CatsModel;
using CatsModel.Filters;
using CatsUtil;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.KeyBox;
using System.Text.Json;

namespace CatsServer.Controllers;

public class CatsController : Controller
{
    [Route("/breeds/{filter?}")]
    public async Task GetBreedsAsync(string? filter)
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        KeyRingJsonConverterFactory converter = HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>();
        jsonSerializerOptions.Converters.Add(converter);
        BreedListFilter? filterObject = null;
        if (filter is { })
        {
            filterObject = JsonSerializer.Deserialize<BreedListFilter>(filter, jsonSerializerOptions);
        }
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Breed>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetBreedsAsync(filterObject), jsonSerializerOptions);
    }

    [Route("/catteries/{filter?}")]
    public async Task GetCatteriesAsync(string? filter)
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        KeyRingJsonConverterFactory converter = HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>();
        jsonSerializerOptions.Converters.Add(converter);
        CatteryListFilter? filterObject = null;
        if (filter is { })
        {
            filterObject = JsonSerializer.Deserialize<CatteryListFilter>(filter, jsonSerializerOptions);
        }
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cattery>>(
            HttpContext.RequestServices.GetRequiredService<Storage>().GetCatteriesAsync(filterObject), jsonSerializerOptions);
    }

    [Route("/cats/{filter?}")]
    public async Task GetCatsAsync(string? filter)
    {
        JsonSerializerOptions jsonSerializerOptions = new();
        KeyRingJsonConverterFactory converter = HttpContext.RequestServices.GetRequiredService<KeyRingJsonConverterFactory>();
        CatsUtil.ObjectCache cache = new();
        converter.PrimaryKeyFound += arg =>
        {
            if (!arg.IsReading)
            {
                if(cache.TryGet(arg.Value.GetType(), arg.KeyRing, out object _))
                {
                    arg.Interrupt = true;
                }
                else
                {
                    cache.Add(arg.Value.GetType(), arg.KeyRing, arg.Value);
                }
            }
        };
        jsonSerializerOptions.Converters.Add(converter);
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<DateOnlyJsonConverter>());
        jsonSerializerOptions.Converters.Add(HttpContext.RequestServices.GetRequiredService<EnumJsonConverter<Gender>>());
        CatListFilter? filterObject = null;
        if (filter is { })
        {
            filterObject = JsonSerializer.Deserialize<CatListFilter>(filter, jsonSerializerOptions);
            if (filterObject?.Ancestor is { })
            {
                await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cat>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetDescendantsAsync(filterObject), jsonSerializerOptions);
                return;
            }
            if (filterObject?.Descendant is { })
            {
                await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cat>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetAncestorsAsync(filterObject), jsonSerializerOptions);
                return;
            }
        }
        await HttpContext.Response.WriteAsJsonAsync<IAsyncEnumerable<Cat>>(HttpContext.RequestServices.GetRequiredService<Storage>().GetCatsAsync(filterObject), jsonSerializerOptions);
    }


}

