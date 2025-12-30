using LinguaAI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// API Base URL configuration - read from environment variable
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var authUserId = builder.Configuration["Auth:UserId"];
var authApiKey = builder.Configuration["Auth:ApiKey"];

if (string.IsNullOrEmpty(apiBaseUrl))
{
    apiBaseUrl = "http://localhost:5000";
    Console.WriteLine("WARNING: ApiBaseUrl not configured. Using default: " + apiBaseUrl);
}
else
{
    Console.WriteLine("API Base URL: " + apiBaseUrl);
}

if (string.IsNullOrEmpty(authUserId) || string.IsNullOrEmpty(authApiKey))
{
    Console.WriteLine("CRITICAL: Auth credentials missing in configuration!");
    Console.WriteLine($"Auth:UserId present: {!string.IsNullOrEmpty(authUserId)}");
    Console.WriteLine($"Auth:ApiKey present: {!string.IsNullOrEmpty(authApiKey)}");
    Console.WriteLine("Ensure Auth__UserId and Auth__ApiKey environment variables are set.");
}
else
{
    Console.WriteLine($"Auth configuration loaded for User: {authUserId}, Key Length: {authApiKey.Length}");
}

// Register HttpClientFactory with ApiService
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
