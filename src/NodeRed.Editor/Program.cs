using NodeRed.Editor.Components;
using NodeRed.Editor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Add EditorState as scoped service (per-connection in Blazor Server)
builder.Services.AddScoped<EditorState>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri("http://localhost:5000/"); // Will be configured properly
    return new EditorState(httpClient);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
