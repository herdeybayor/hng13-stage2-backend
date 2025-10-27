using Scalar.AspNetCore;
using CountryCurrencyAPI.Data;
using CountryCurrencyAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use property names as-is
    });

// Database configuration
// Railway provides MYSQL_URL in format: mysql://user:password@host:port/database
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL");

// If MYSQL_URL is not available, try individual variables or fallback to config
if (string.IsNullOrEmpty(connectionString))
{
    var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT");
    var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE");
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
    var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");

    if (!string.IsNullOrEmpty(mysqlHost))
    {
        connectionString = $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDatabase};User={mysqlUser};Password={mysqlPassword};SslMode=Required;";
    }
    else
    {
        // Fallback to appsettings
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    }
}
else
{
    // Parse Railway's mysql:// URL format
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};User={userInfo[0]};Password={userInfo[1]};SslMode=Required;";
}

// Log connection attempt (without password)
var safeConnectionString = connectionString?.Split("Password=")[0] + "Password=***";
Console.WriteLine($"Attempting to connect with: {safeConnectionString}");

// Use a specific MySQL version instead of AutoDetect to avoid connection issues during startup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// HttpClient configuration with timeout
builder.Services.AddHttpClient<ICountryService, CountryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IImageService, ImageService>();

// CORS (if needed for frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
