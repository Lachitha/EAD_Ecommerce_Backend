using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDbConsoleApp; // Ensure this namespace is correct based on your project structure
using MongoDbConsoleApp.Services; // Add this line to use UserService and ProductService
using MongoDbConsoleApp.Helpers; // Add this line to use JwtHelper
using MongoDB.Driver; // Required for IMongoDatabase
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration values (ensure these are set in your appsettings.json)
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
var key = jwtSettings["Key"];

// Check for null settings to avoid runtime errors
if (string.IsNullOrEmpty(key))
{
    throw new ArgumentNullException("JWT Key is not set in appsettings.json.");
}
if (string.IsNullOrEmpty(issuer))
{
    throw new ArgumentNullException("JWT Issuer is not set in appsettings.json.");
}
if (string.IsNullOrEmpty(audience))
{
    throw new ArgumentNullException("JWT Audience is not set in appsettings.json.");
}

// Configure MongoDB Service
builder.Services.AddSingleton<MongoDbService>(sp =>
{
    return new MongoDbService(
        mongoSettings["ConnectionString"] ?? throw new ArgumentNullException("MongoDB Connection String is not set in appsettings.json."),
        mongoSettings["DatabaseName"] ?? throw new ArgumentNullException("MongoDB Database Name is not set in appsettings.json.")
    );
});

// Register services for dependency injection
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProductService>(); // Register ProductService
builder.Services.AddScoped<OrderService>(); // Register OrderService
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CartService>();

// Register JwtHelper with required parameters
builder.Services.AddSingleton<JwtHelper>(sp =>
{
    return new JwtHelper(key, issuer, audience); // Pass the required parameters
});

// Configure services for JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAll",
           builder => builder.AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader());
   });

// Add controllers (this is necessary for API routing)
builder.Services.AddControllers();

var app = builder.Build();

// Create a scope to resolve the MongoDbService and call PingAsync
using (var scope = app.Services.CreateScope())
{
    var mongoService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
    await mongoService.PingAsync(); // Ensure this method works correctly with async
}

// Configure middleware
app.UseCors("AllowAll"); // Use the CORS policy

app.UseAuthentication();
app.UseAuthorization();

// Map controllers to routes
app.MapControllers(); // This is essential for your endpoints to be accessible

app.Run();
