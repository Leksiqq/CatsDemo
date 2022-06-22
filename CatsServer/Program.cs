using CatsModel;
using CatsServer;
using CatsUtil;
using Net.Leksi.KeyBox;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyBox(config => {
    config.AddPrimaryKey(typeof(Cat), new Dictionary<string, object> { { "IdCat", typeof(int) }, { "IdCattery", "/Cattery/IdCattery" } });
    config.AddPrimaryKey(typeof(Breed), new Dictionary<string, object> { { "IdBreed", "/Code" }, { "IdGroup", "/Group" } });
    config.AddPrimaryKey(typeof(Cattery), new Dictionary<string, object> { { "IdCattery", typeof(int) } });
    config.AddPrimaryKey(typeof(Litter), new Dictionary<string, object> { { "IdLitter", "/Order" }, { "IdFemale", "/Female/IdCat" }, { "IdFemaleCattery", "/Female/IdCattery" } });
});

builder.Services.AddSingleton<Storage>(serviceProvider => new Storage(serviceProvider, builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddTransient<KeyRingJsonConverterFactory>();
builder.Services.AddTransient(typeof(KeyRingJsonConverter<>));
builder.Services.AddTransient<DateOnlyJsonConverter>();
builder.Services.AddTransient(typeof(EnumJsonConverter<>));
builder.Services.AddTransient<ObjectCache>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
