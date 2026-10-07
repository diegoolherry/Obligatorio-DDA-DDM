using Web_App.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Cuando exista un cliente HTTP de la API, deberá deserializar RolIngreso con
// JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false), igual que la API.
// Este portal todavía no consume la API; el comentario no configura un cliente.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
