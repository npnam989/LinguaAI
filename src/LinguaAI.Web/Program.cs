using LinguaAI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// API Base URL configuration - read from environment variable
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    apiBaseUrl = "http://localhost:5000";
    Console.WriteLine("WARNING: ApiBaseUrl not configured. Using default: " + apiBaseUrl);
    Console.WriteLine("Set environment variable: ApiBaseUrl=https://your-api.railway.app");
}
else
{
    Console.WriteLine("API Base URL: " + apiBaseUrl);
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
