using MongoDB.Driver;
using AgenticOrderingSystem.API.Configuration;
using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Database service for MongoDB operations
/// </summary>
public interface IDatabaseService
{
    // ProductDesigner_DB collections
    IMongoCollection<Product> Products { get; }
    IMongoCollection<Category> Categories { get; }

    // CMP_DB collections
    IMongoCollection<User> Users { get; }
    IMongoCollection<Order> Orders { get; }
    IMongoCollection<AIAgentSession> AIAgentSessions { get; }
    IMongoCollection<UserSession> UserSessions { get; }
    IMongoCollection<AuditLog> AuditLogs { get; }

    // Health check
    Task<bool> TestConnectionAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly IMongoDatabase _productDesignerDatabase;
    private readonly IMongoDatabase _cmpDatabase;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(DatabaseSettings settings, ILogger<DatabaseService> logger)
    {
        _logger = logger;

        try
        {
            // Initialize ProductDesigner database
            var productDesignerClient = new MongoClient(settings.ProductDesignerConnectionString);
            _productDesignerDatabase = productDesignerClient.GetDatabase(settings.ProductDesignerDatabaseName);

            // Initialize CMP database
            var cmpClient = new MongoClient(settings.CMPConnectionString);
            _cmpDatabase = cmpClient.GetDatabase(settings.CMPDatabaseName);

            _logger.LogInformation("Database connections initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database connections");
            throw;
        }
    }

    // ProductDesigner_DB collections
    public IMongoCollection<Product> Products => 
        _productDesignerDatabase.GetCollection<Product>(CollectionNames.Products);

    public IMongoCollection<Category> Categories => 
        _productDesignerDatabase.GetCollection<Category>(CollectionNames.Categories);

    // CMP_DB collections
    public IMongoCollection<User> Users => 
        _cmpDatabase.GetCollection<User>(CollectionNames.Users);

    public IMongoCollection<Order> Orders => 
        _cmpDatabase.GetCollection<Order>(CollectionNames.Orders);

    public IMongoCollection<AIAgentSession> AIAgentSessions => 
        _cmpDatabase.GetCollection<AIAgentSession>(CollectionNames.AIAgentSessions);

    public IMongoCollection<UserSession> UserSessions => 
        _cmpDatabase.GetCollection<UserSession>("userSessions");

    public IMongoCollection<AuditLog> AuditLogs => 
        _cmpDatabase.GetCollection<AuditLog>("auditLogs");

    /// <summary>
    /// Test database connectivity
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Test ProductDesigner DB
            await _productDesignerDatabase.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
            _logger.LogInformation("ProductDesigner_DB connection successful");

            // Test CMP DB
            await _cmpDatabase.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
            _logger.LogInformation("CMP_DB connection successful");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }
}
