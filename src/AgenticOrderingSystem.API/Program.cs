using DotNetEnv;
using AgenticOrderingSystem.API.Configuration;
using AgenticOrderingSystem.API.Services;

// Load environment variables from .env file in the root directory
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"✅ Loaded environment variables from: {envPath}");
}
else
{
    Console.WriteLine($"⚠️ .env file not found at: {envPath}");
    // Try loading from current directory as fallback
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Configure Database Settings
builder.Services.AddSingleton<DatabaseSettings>(provider =>
{
    var settings = new DatabaseSettings
    {
        ProductDesignerConnectionString = Environment.GetEnvironmentVariable("MONGODB_PRODUCTDESIGNER_CONNECTION") 
            ?? throw new InvalidOperationException("MONGODB_PRODUCTDESIGNER_CONNECTION environment variable is not set"),
        CMPConnectionString = Environment.GetEnvironmentVariable("MONGODB_CMP_CONNECTION") 
            ?? throw new InvalidOperationException("MONGODB_CMP_CONNECTION environment variable is not set")
    };
    return settings;
});

// Register Database Service
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

// Register Data Seeding Service
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<AgenticOrderingSystem.API.Data.TeamBasedOrderSeeder>();

// Register Business Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Register MCP Services
builder.Services.AddScoped<AgenticOrderingSystem.API.MCP.Interfaces.IMCPOrchestrator, AgenticOrderingSystem.API.MCP.Services.MCPOrchestrator>();
builder.Services.AddScoped<AgenticOrderingSystem.API.MCP.Interfaces.IPerplexityAIService, AgenticOrderingSystem.API.MCP.Services.PerplexityAIService>();

// Register MCP Tools
builder.Services.AddScoped<AgenticOrderingSystem.API.MCP.Tools.OrderManagement.GetUserOrdersTool>();
builder.Services.AddScoped<AgenticOrderingSystem.API.MCP.Tools.OrderManagement.GetOrderDetailsTool>();
builder.Services.AddScoped<AgenticOrderingSystem.API.MCP.Tools.OrderManagement.AnalyzeOrderFailuresTool>();

// Add HTTP Client for AI services
builder.Services.AddHttpClient<AgenticOrderingSystem.API.MCP.Services.PerplexityAIService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ?? new[] { "http://localhost:4200" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}

app.UseAuthorization();

app.MapControllers();

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var isConnected = await databaseService.TestConnectionAsync();
        if (isConnected)
        {
            logger.LogInformation("🎉 Database connections verified successfully!");
        }
        else
        {
            logger.LogError("❌ Database connection test failed!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error testing database connections");
    }
}

app.Run();
