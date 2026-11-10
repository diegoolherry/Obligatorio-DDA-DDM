using API_Berruti.Data;
using API_Berruti.Security.Authentication;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
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
builder.Services.AddScoped<IPreparacionDemoRepository, PreparacionDemoRepository>();
builder.Services.AddScoped<IPreparacionDemoService, PreparacionDemoService>();
// EF diagnostics can include provider details; keep authentication failures sanitized.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);

var app = builder.Build();

if (args.Contains(ReinicioLocal.Argumento))
    return await ReinicioLocal.EjecutarAsync(app.Services, builder.Configuration, app.Environment);

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
