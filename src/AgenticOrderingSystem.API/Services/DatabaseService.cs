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
            var mongoClient = new MongoClient(settings.ProductDesignerConnectionString);
            
            // Initialize ProductDesigner database
            _productDesignerDatabase = mongoClient.GetDatabase(settings.ProductDesignerDatabaseName);

            // Initialize CMP database - reuse the same client if connection strings are the same
            if (settings.CMPConnectionString != settings.ProductDesignerConnectionString)
            {
                var cmpClient = new MongoClient(settings.CMPConnectionString);
                _cmpDatabase = cmpClient.GetDatabase(settings.CMPDatabaseName);
            }
            else
            {
                _cmpDatabase = mongoClient.GetDatabase(settings.CMPDatabaseName);
            }

            _logger.LogInformation("Database connections initialized successfully with optimized client reuse");
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
