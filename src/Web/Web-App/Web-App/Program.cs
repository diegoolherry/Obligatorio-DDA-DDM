using Web_App.Components;
using Web_App.Services.Authentication;
using Web_App.Security;

var builder = WebApplication.CreateBuilder(args);

// Scoped: memoria aislada por circuito InteractiveServer, nunca cookie ni singleton.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ISesionCliente, SesionCliente>();
builder.Services.AddHttpClient("Autenticacion", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.MaxResponseContentBufferSize = 64 * 1024;
})
.RemoveAllLoggers()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = false
});
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
app.UseMiddleware<LimiteIngresoHttps>();
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
