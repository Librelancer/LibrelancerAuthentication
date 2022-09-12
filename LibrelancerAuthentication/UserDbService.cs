using Microsoft.AspNetCore.Identity;

namespace LibrelancerAuthentication;

public class UserDbService
{
    private string dbPath;
    private PasswordHasher<User> passwordHasher;
    public UserDbService(string dbPath, PasswordHasher<User> passwordHasher)
    {
        this.dbPath = dbPath;
        this.passwordHasher = passwordHasher;
    }

    public async Task<User> Login(string username, string password)
    {
        await using var context = new UserDbContext(dbPath);
        var user = context.Users.FirstOrDefault(x => x.Username.Equals(username));
        if (user != null)
        {
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, password);
                await context.SaveChangesAsync();
            }

            if (result == PasswordVerificationResult.Success ||
                result == PasswordVerificationResult.SuccessRehashNeeded)
                return user;
        }

        await Task.Delay(3000);
        return null;
    }
    
    public async Task<bool> ChangePassword(string username, string oldpassword, string newpassword)
    {
        await using var context = new UserDbContext(dbPath);
        var user = context.Users.FirstOrDefault(x => x.Username.Equals(username));
        if (user != null)
        {
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldpassword);
            if (result == PasswordVerificationResult.Success ||
                result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, newpassword);
                await context.SaveChangesAsync();
                return true;
            }
        }
        await Task.Delay(3000);
        return false;
    }

    public async Task<bool> Register(string username, string password)
    {
        await using var context = new UserDbContext(dbPath);
        if (context.Users.Any(x => x.Username.Equals(username)))
            return false;
        var newUser = new User {Username = username, Guid = Guid.NewGuid().ToString()};
        newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);
        context.Users.Add(newUser);
        await context.SaveChangesAsync();
        return true;
    }
}