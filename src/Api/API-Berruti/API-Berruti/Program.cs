using API_Berruti.Data;
using API_Berruti.Security.Authentication;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
using API_Berruti.Repositories.Location;
using API_Berruti.Services.Location;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using API_Berruti.Repositories.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AgregarAutenticacionLocal(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IParadasRepository, ParadasRepository>();
builder.Services.AddScoped<ITramosRepository, TramosRepository>();
builder.Services.AddScoped<IParadasService, ParadasService>();
builder.Services.AddScoped<ITramosService, TramosService>();
builder.Services.AddScoped<IServiciosRepository, ServiciosRepository>();
builder.Services.AddScoped<IServiciosService, ServiciosService>();
builder.Services.AddScoped<ITarifasRepository, TarifasRepository>();
builder.Services.AddScoped<ITarifasService, TarifasService>();
builder.Services.AddScoped<IBusquedaItinerariosRepository, BusquedaItinerariosRepository>();
builder.Services.AddScoped<IItinerariosService, ItinerariosService>();
builder.Services.AddScoped<IIntentosCompraRepository, IntentosCompraRepository>();
builder.Services.AddScoped<IIntentosCompraService, IntentosCompraService>();
builder.Services.AddSingleton(ParametrosCompra.Desde(builder.Configuration));
builder.Services.AddSingleton<ParametrosMercadoPago>(servicios =>
    ParametrosMercadoPago.Desde(servicios.GetRequiredService<IConfiguration>()));
builder.Services.AddHttpClient<IMercadoPagoCheckoutClient, MercadoPagoCheckoutClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IGeneracionServiciosRepository, GeneracionServiciosRepository>();
builder.Services.AddScoped<IGeneracionServiciosService, GeneracionServiciosService>();
builder.Services.AddScoped<ICatalogoAdministracionRepository, CatalogoAdministracionRepository>();
builder.Services.AddScoped<ICatalogoAdministracionService, CatalogoAdministracionService>();
// Parámetros académicos de ubicación (appsettings.json, sección Ubicacion); valores inválidos detienen el arranque.
builder.Services.AddSingleton(ParametrosUbicacion.Desde(builder.Configuration));
builder.Services.AddScoped<IUbicacionRepository, UbicacionRepository>();
builder.Services.AddScoped<IUbicacionService, UbicacionService>();
// EF diagnostics can include provider details; keep authentication failures sanitized.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);

var app = builder.Build();

if (args.Contains(ReinicioLocal.Argumento))
    return await ReinicioLocal.EjecutarAsync(app.Services, builder.Configuration, app.Environment);

// El arranque normal exige una clave JWT válida (Jwt__Key, fuera de Git); el reinicio local no firma tokens.
try { ConfiguracionJwt.Leer(builder.Configuration); }
catch (InvalidOperationException error)
{
    Console.Error.WriteLine("Configuración JWT inválida: " + error.Message);
    return 2;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<LimiteAutenticacion>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
return 0;
