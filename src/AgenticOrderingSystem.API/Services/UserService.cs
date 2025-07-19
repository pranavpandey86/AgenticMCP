using MongoDB.Driver;
using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Service for managing users and authentication
/// </summary>
public interface IUserService
{
    // User CRUD operations
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<List<User>> GetUsersByDepartmentAsync(string department);
    Task<List<User>> GetUsersByRoleAsync(string role);
    Task<List<User>> GetApproversForDepartmentAsync(string department, decimal minimumApprovalLimit = 0);
    
    // User management
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId);
    
    // User search and filtering
    Task<List<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<List<User>> GetActiveUsersAsync(int page = 1, int pageSize = 20);
    
    // Approval authority
    Task<bool> CanUserApproveAmountAsync(string userId, decimal amount);
    Task<List<User>> GetApprovalChainAsync(string userId, string department);
}

public class UserService : IUserService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<UserService> _logger;

    public UserService(IDatabaseService databaseService, ILogger<UserService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            return await _databaseService.Users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            return await _databaseService.Users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email: {Email}", email);
            throw;
        }
    }

    public async Task<List<User>> GetUsersByDepartmentAsync(string department)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Department, department),
                Builders<User>.Filter.Eq(u => u.IsActive, true)
            );
            
            return await _databaseService.Users
                .Find(filter)
                .SortBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users by department: {Department}", department);
            throw;
        }
    }

    public async Task<List<User>> GetUsersByRoleAsync(string role)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Role, role),
                Builders<User>.Filter.Eq(u => u.IsActive, true)
            );
            
            return await _databaseService.Users
                .Find(filter)
                .SortBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users by role: {Role}", role);
            throw;
        }
    }

    public async Task<List<User>> GetApproversForDepartmentAsync(string department, decimal minimumApprovalLimit = 0)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Department, department),
                Builders<User>.Filter.Eq(u => u.IsActive, true),
                Builders<User>.Filter.Eq(u => u.ApprovalAuthority.CanApprove, true),
                Builders<User>.Filter.Gte(u => u.ApprovalAuthority.MaxApprovalAmount, minimumApprovalLimit)
            );
            
            return await _databaseService.Users
                .Find(filter)
                .SortByDescending(u => u.ApprovalAuthority.MaxApprovalAmount)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get approvers for department: {Department}", department);
            throw;
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;

            await _databaseService.Users.InsertOneAsync(user);
            
            _logger.LogInformation("User created successfully: {UserId} - {Email}", user.Id, user.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Email}", user.Email);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _databaseService.Users.ReplaceOneAsync(filter, user);
            
            _logger.LogInformation("User updated successfully: {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var result = await _databaseService.Users.DeleteOneAsync(filter);
            
            var deleted = result.DeletedCount > 0;
            if (deleted)
            {
                _logger.LogInformation("User deleted successfully: {UserId}", userId);
            }
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.IsActive, true),
                Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.FirstName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    Builders<User>.Filter.Regex(u => u.LastName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    Builders<User>.Filter.Regex(u => u.Department, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                )
            );

            var skip = (page - 1) * pageSize;
            
            return await _databaseService.Users
                .Find(filter)
                .SortBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<User>> GetActiveUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.IsActive, true);
            var skip = (page - 1) * pageSize;
            
            return await _databaseService.Users
                .Find(filter)
                .SortBy(u => u.Department)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active users");
            throw;
        }
    }

    public async Task<bool> CanUserApproveAmountAsync(string userId, decimal amount)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            return user.ApprovalAuthority.CanApprove && 
                   user.ApprovalAuthority.MaxApprovalAmount >= amount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check approval authority for user: {UserId}, Amount: {Amount}", userId, amount);
            return false;
        }
    }

    public async Task<List<User>> GetApprovalChainAsync(string userId, string department)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return new List<User>();

            var approvalChain = new List<User>();
            
            // Start with immediate manager
            var currentUserId = user.ManagerId;
            var visited = new HashSet<string> { userId }; // Prevent circular references

            while (!string.IsNullOrEmpty(currentUserId) && !visited.Contains(currentUserId))
            {
                visited.Add(currentUserId);
                var manager = await GetUserByIdAsync(currentUserId);
                
                if (manager == null || !manager.IsActive)
                    break;

                if (manager.ApprovalAuthority.CanApprove)
                {
                    approvalChain.Add(manager);
                }

                // Move up the chain
                currentUserId = manager.ManagerId;
            }

            return approvalChain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get approval chain for user: {UserId}", userId);
            throw;
        }
    }
}
